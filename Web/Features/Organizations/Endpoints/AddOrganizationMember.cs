using System.Data;
using System.Net.Mail;
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
using VsaTemplate.Web.Features.Organizations.Authorization;
using VsaTemplate.Web.Features.Organizations.Models;
using VsaTemplate.Web.Features.Organizations.Services;
using VsaTemplate.Web.Features.Shared.Exceptions;
using VsaTemplate.Web.Infrastructure.Exceptions;

namespace VsaTemplate.Web.Features.Organizations.Endpoints;

[Handler]
[MapPut("/api/organizations/{organizationId}/users")]
public sealed partial class AddOrganizationMember(
	CurrentUserService currentUserService,
	CurrentOrganizationUserService currentOrganizationUserService,
	DbContext context,
	PermissionCacheInvalidator permissionCacheInvalidator
)
{
	[Validate]
	public sealed partial record Request : IAuthorizedRequest, IOrganizationRequest, IValidationTarget<Request>
	{
		public static OrganizationPermission OrganizationPermission => OrganizationPermission.UserManagement;

		[FromRoute]
		public required OrganizationId OrganizationId { get; init; }

		[FromBody]
		public required RequestBody Body { get; init; }
	}

	[Validate]
	public sealed partial record RequestBody : IValidationTarget<RequestBody>
	{
		[NotEmpty]
		[MaxLength(500)]
		public required string EmailAddress { get; init; }
		public required OrganizationRoleId RoleId { get; init; }
	}

	public sealed record Response
	{
		public required UserId UserId { get; init; }
	}

	private async ValueTask<Response> HandleAsync([AsParameters] Request request, CancellationToken token)
	{
		var email = request.Body.EmailAddress.Trim();
		if (!MailAddress.TryCreate(email, out var address) || !string.Equals(address.Address, email, StringComparison.OrdinalIgnoreCase))
			throw new InvalidMemberInputException("A valid email address is required.");
		await using var transaction = await context.BeginTransactionAsync(IsolationLevel.Serializable, token);
		var role = await OrganizationManagement.GetRole(context, request.OrganizationId, request.Body.RoleId, token);
		await OrganizationManagement.DemandDelegablePermissions(currentOrganizationUserService, request.OrganizationId,
			OrganizationManagement.ReadPermissions(role.PermissionsJson));
		var user = await context.AccessControl.Users.FirstOrDefaultAsync(u => u.EmailAddress == email && u.IsActive && u.ApiKey == null, token)
			?? throw new NotFoundException("Tenant member", "Add this email address to the parent tenant first.");
		await OrganizationManagement.EnsureTenantMember(context, request.OrganizationId, user.UserId, token);
		await context.OrganizationRoleUsers.Merge().Using([
			new Database.Models.OrganizationRoleUser
			{
				OrganizationRoleId = request.Body.RoleId,
				UserId = user.UserId,
				EditedUserId = await currentUserService.GetLoggedInUserId(),
			},
		])
			.OnTargetKey()
			.InsertWhenNotMatched()
			.MergeAsync(token);
		await transaction.CommitAsync(token);
		await permissionCacheInvalidator.InvalidateOrganization(request.OrganizationId, CancellationToken.None);
		return new() { UserId = user.UserId };
	}
}

file sealed class InvalidMemberInputException(string message) : VsaTemplateException(message, 400);
