using System.Data;
using Immediate.Apis.Shared;
using Immediate.Handlers.Shared;
using LinqToDB;
using Microsoft.AspNetCore.Mvc;
using VsaTemplate.Web.Database;
using VsaTemplate.Web.Features.AccessControl.Authorization;
using VsaTemplate.Web.Features.AccessControl.Models;
using VsaTemplate.Web.Features.AccessControl.Services;
using VsaTemplate.Web.Features.Organizations.Authorization;
using VsaTemplate.Web.Features.Organizations.Models;
using VsaTemplate.Web.Features.Organizations.Services;

namespace VsaTemplate.Web.Features.Organizations.Endpoints;

[Handler]
[MapPut("/api/organizations/{organizationId}/roles/{roleId}/users/{userId}")]
public sealed partial class AddUserToOrganization(
	CurrentUserService currentUserService,
	CurrentOrganizationUserService currentOrganizationUserService,
	DbContext context,
	PermissionCacheInvalidator permissionCacheInvalidator
)
{
	public sealed record Request : IAuthorizedRequest, IOrganizationRequest
	{
		public static OrganizationPermission OrganizationPermission => OrganizationPermission.UserManagement;

		[FromRoute]
		public required OrganizationId OrganizationId { get; init; }

		[FromRoute]
		public required OrganizationRoleId RoleId { get; init; }

		[FromRoute]
		public required UserId UserId { get; init; }
	}

	private async ValueTask HandleAsync([AsParameters] Request request, CancellationToken token)
	{
		await using var transaction = await context.BeginTransactionAsync(IsolationLevel.Serializable, token);
		var role = await OrganizationManagement.GetRole(context, request.OrganizationId, request.RoleId, token);

		await OrganizationManagement.DemandDelegablePermissions(currentOrganizationUserService, request.OrganizationId,
			OrganizationManagement.ReadPermissions(role.PermissionsJson));

		await OrganizationManagement.EnsureActiveUser(context, request.UserId, token);

		await context.OrganizationRoleUsers
			.Merge().Using([
			new Database.Models.OrganizationRoleUser
			{
				OrganizationRoleId = request.RoleId,
				UserId = request.UserId,
				EditedUserId = currentUserService.GetLoggedInUserId(),
			},
		])
			.OnTargetKey()
			.InsertWhenNotMatched()
			.MergeAsync(token);

		await transaction.CommitAsync(token);
		await permissionCacheInvalidator.InvalidateOrganization(request.OrganizationId);
	}
}
