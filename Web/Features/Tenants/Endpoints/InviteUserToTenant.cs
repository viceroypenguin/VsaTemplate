using System.Data;
using Immediate.Apis.Shared;
using Immediate.Handlers.Shared;
using Immediate.Validations.Shared;
using LinqToDB;
using LinqToDB.Async;
using Microsoft.AspNetCore.Mvc;
using VsaTemplate.Web.Database;
using VsaTemplate.Web.Features.AccessControl.Authorization;
using VsaTemplate.Web.Features.AccessControl.Models;
using VsaTemplate.Web.Features.AccessControl.Services;
using VsaTemplate.Web.Features.Shared.Exceptions;
using VsaTemplate.Web.Features.Shared.Validators;
using VsaTemplate.Web.Features.Tenants.Authorization;
using VsaTemplate.Web.Features.Tenants.Models;
using VsaTemplate.Web.Features.Tenants.Services;

namespace VsaTemplate.Web.Features.Tenants.Endpoints;

[Handler]
[MapPut("/api/tenants/{tenantId}/users")]
public sealed partial class InviteUserToTenant(
	CurrentUserService currentUserService,
	CurrentTenantUserService currentTenantUserService,
	DbContext context,
	PermissionCacheInvalidator cacheInvalidator
)
{
	private async ValueTask<Response> HandleAsync([AsParameters] Request request, CancellationToken token)
	{
		var emailAddress = request.Body.EmailAddress.Trim();

		var actorId = currentUserService.GetLoggedInUserId();
		var actorPermissions = await currentTenantUserService.GetLoggedInUserPermissions(request.TenantId);

		await using var transaction = await context.BeginTransactionAsync(IsolationLevel.Serializable, token);

		var role = await context.TenantRoles
				.FirstOrDefaultAsync(
					r => r.TenantId == request.TenantId && r.TenantRoleId == request.Body.RoleId,
					token
				)
			?? throw new NotFoundException(nameof(Database.Models.TenantRole));

		TenantManagement.EnsureCanGrant(actorPermissions, TenantManagement.ReadPermissions(role.PermissionsJson));

		var user = await context.AccessControl.Users
			.FirstOrDefaultAsync(u => u.EmailAddress == emailAddress, token);

		if (user is { IsActive: false }
			|| (user is not null
				&& await context.AccessControl.ApiKeys.AnyAsync(k => k.ApiKeyId == user.UserId, token)))
		{
			throw new ConflictException("This email address is unavailable for membership.");
		}

		var userId = user?.UserId
			?? UserId.From(
				await context.InsertWithInt32IdentityAsync(
					new Database.Models.AccessControl.User { EmailAddress = emailAddress, IsActive = true },
					token: token
				)
			);

		await context.TenantRoleUsers
			.Merge()
			.Using(
				[
					new Database.Models.TenantRoleUser
					{
						TenantRoleId = request.Body.RoleId,
						UserId = userId,
						EditedUserId = actorId,
					},
				]
			)
			.OnTargetKey()
			.InsertWhenNotMatched()
			.MergeAsync(token);

		await transaction.CommitAsync(token);
		await cacheInvalidator.InvalidateUser(userId);
		return new() { UserId = userId };
	}

	[Validate]
	public sealed partial record Request : IAuthorizedRequest, ITenantRequest, IValidationTarget<Request>
	{
		[FromBody]
		public required RequestBody Body { get; init; }

		public static TenantPermission TenantPermission => TenantPermission.UserManagement;

		[FromRoute]
		public required TenantId TenantId { get; init; }
	}

	[Validate]
	public sealed partial record RequestBody : IValidationTarget<RequestBody>
	{
		[NotEmpty]
		[TrimmedMaxLength(500)]
		[BareEmailAddress]
		public required string EmailAddress { get; init; }

		public required TenantRoleId RoleId { get; init; }
	}

	public sealed record Response
	{
		public required UserId UserId { get; init; }
	}
}
