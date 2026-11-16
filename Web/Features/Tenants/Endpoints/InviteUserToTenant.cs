using System.Data;
using System.Net.Mail;
using Immediate.Apis.Shared;
using Immediate.Handlers.Shared;
using LinqToDB;
using LinqToDB.Async;
using Microsoft.AspNetCore.Mvc;
using VsaTemplate.Web.Database;
using VsaTemplate.Web.Features.AccessControl.Authorization;
using VsaTemplate.Web.Features.AccessControl.Models;
using VsaTemplate.Web.Features.AccessControl.Services;
using VsaTemplate.Web.Features.Shared.Exceptions;
using VsaTemplate.Web.Features.Tenants.Authorization;
using VsaTemplate.Web.Features.Tenants.Exceptions;
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
	public sealed record Request : IAuthorizedRequest, ITenantRequest
	{
		public static TenantPermission TenantPermission => TenantPermission.UserManagement;
		[FromRoute] public required TenantId TenantId { get; init; }
		[FromBody] public required RequestBody Body { get; init; }
	}

	public sealed record RequestBody
	{
		public required string EmailAddress { get; init; }
		public required TenantRoleId RoleId { get; init; }
	}

	public sealed record Response
	{
		public required UserId UserId { get; init; }
	}

	private async ValueTask<Response> HandleAsync([AsParameters] Request request, CancellationToken token)
	{
		var emailAddress = request.Body.EmailAddress?.Trim();

		if (string.IsNullOrEmpty(emailAddress) || emailAddress.Length > 500
			|| !MailAddress.TryCreate(emailAddress, out var address)
			|| !string.Equals(address.Address, emailAddress, StringComparison.OrdinalIgnoreCase))
			throw new TenantValidationException("Enter a valid email address of at most 500 characters.");

		var actorId = currentUserService.GetLoggedInUserId();
		var actorPermissions = await currentTenantUserService.GetLoggedInUserPermissions(request.TenantId);

		await using var transaction = await context.BeginTransactionAsync(IsolationLevel.Serializable, token);

		var role = await context.TenantRoles
			.FirstOrDefaultAsync(
			r => r.TenantId == request.TenantId && r.TenantRoleId == request.Body.RoleId, token)
			?? throw new NotFoundException(nameof(Database.Models.TenantRole));

		TenantManagement.EnsureCanGrant(actorPermissions, TenantManagement.ReadPermissions(role.PermissionsJson));

		var user = await context.AccessControl.Users
			.FirstOrDefaultAsync(u => u.EmailAddress == emailAddress, token);

		if (user is { IsActive: false }
			|| (user is not null && await context.AccessControl.ApiKeys.AnyAsync(k => k.ApiKeyId == user.UserId, token)))
			throw new ConflictException("This email address is unavailable for membership.");

		var userId = user?.UserId ?? UserId.From(await context.InsertWithInt32IdentityAsync(
			new Database.Models.AccessControl.User { EmailAddress = emailAddress, IsActive = true }, token: token));

		await context.TenantRoleUsers
			.Merge().Using([
			new Database.Models.TenantRoleUser
			{
				TenantRoleId = request.Body.RoleId,
				UserId = userId,
				EditedUserId = actorId,
			},
		])
			.OnTargetKey()
			.InsertWhenNotMatched()
			.MergeAsync(token);

		await transaction.CommitAsync(token);
		await cacheInvalidator.InvalidateUser(userId, CancellationToken.None);
		return new() { UserId = userId };
	}
}
