using System.Data;
using Immediate.Apis.Shared;
using Immediate.Handlers.Shared;
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

namespace VsaTemplate.Web.Features.Organizations.Endpoints;

[Handler]
[MapDelete("/api/organizations/{organizationId}/users/{userId}")]
public sealed partial class RemoveUserFromOrganization(
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
		public required UserId UserId { get; init; }
	}

	private async ValueTask HandleAsync([AsParameters] Request request, CancellationToken token)
	{
		await using var transaction = await context.BeginTransactionAsync(IsolationLevel.Serializable, token);
		var roles = await context.OrganizationRoleUsers
			.Where(u => u.UserId == request.UserId && u.OrganizationRole.OrganizationId == request.OrganizationId)
			.Select(u => u.OrganizationRole)
			.ToListAsync(token);
		if (roles.Count == 0)
			NotFoundException.ThrowNotFoundException("Organization member");
		await OrganizationManagement.DemandDelegablePermissions(currentOrganizationUserService, request.OrganizationId,
			roles.SelectMany(r => OrganizationManagement.ReadPermissions(r.PermissionsJson)).Distinct().ToArray());
		await OrganizationManagement.EnsureAdministratorRemains(context, request.OrganizationId, request.UserId, changingRoleId: null, token);
		var userId = await currentUserService.GetLoggedInUserId();
		var memberships = context.OrganizationRoleUsers
			.Where(u => u.UserId == request.UserId && u.OrganizationRole.OrganizationId == request.OrganizationId);
		await memberships.Set(u => u.EditedUserId, userId).UpdateAsync(token);
		await memberships.DeleteAsync(token);
		await transaction.CommitAsync(token);
		await permissionCacheInvalidator.InvalidateOrganization(request.OrganizationId, CancellationToken.None);
		await permissionCacheInvalidator.InvalidateUser(request.UserId, CancellationToken.None);
	}
}
