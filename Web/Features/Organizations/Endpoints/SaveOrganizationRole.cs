using System.Data;
using System.Text.Json;
using Immediate.Apis.Shared;
using Immediate.Handlers.Shared;
using Immediate.Validations.Shared;
using LinqToDB;
using Microsoft.AspNetCore.Mvc;
using VsaTemplate.Web.Database;
using VsaTemplate.Web.Features.AccessControl.Authorization;
using VsaTemplate.Web.Features.AccessControl.Services;
using VsaTemplate.Web.Features.Organizations.Authorization;
using VsaTemplate.Web.Features.Organizations.Models;
using VsaTemplate.Web.Features.Organizations.Services;

namespace VsaTemplate.Web.Features.Organizations.Endpoints;

[Handler]
[MapPut("/api/organizations/{organizationId}/roles/{roleId}")]
public sealed partial class SaveOrganizationRole(
	DbContext context,
	CurrentUserService currentUserService,
	CurrentOrganizationUserService currentOrganizationUserService,
	PermissionCacheInvalidator permissionCacheInvalidator
)
{
	[Validate]
	public sealed partial record Request : IAuthorizedRequest, IOrganizationRequest, IValidationTarget<Request>
	{
		public static OrganizationPermission OrganizationPermission => OrganizationPermission.UserManagement;

		[FromRoute]
		public required OrganizationId OrganizationId { get; init; }

		[FromRoute]
		public required OrganizationRoleId RoleId { get; init; }

		[FromBody]
		public required AddOrganizationRole.RequestBody Body { get; init; }
	}

	private async ValueTask HandleAsync([AsParameters] Request request, CancellationToken token)
	{
		var name = request.Body.Name.Trim();

		var permissions = request.Body.Permissions
			.Distinct()
			.ToArray();

		await using var transaction = await context.BeginTransactionAsync(IsolationLevel.Serializable, token);
		var role = await OrganizationManagement.GetRole(context, request.OrganizationId, request.RoleId, token);
		var oldPermissions = OrganizationManagement.ReadPermissions(role.PermissionsJson);
		await OrganizationManagement.DemandDelegablePermissions(currentOrganizationUserService, request.OrganizationId, oldPermissions);
		await OrganizationManagement.DemandDelegablePermissions(currentOrganizationUserService, request.OrganizationId, permissions);

		if (oldPermissions.Contains(OrganizationPermission.Admin) && !permissions.Contains(OrganizationPermission.Admin))
			await OrganizationManagement.EnsureAdministratorRemains(context, request.OrganizationId, removingUserId: null, request.RoleId, token);

		var userId = currentUserService.GetLoggedInUserId();

		await context.OrganizationRoles
			.Where(r => r.OrganizationId == request.OrganizationId && r.OrganizationRoleId == request.RoleId)
			.Set(r => r.Name, name)
			.Set(r => r.PermissionsJson, JsonSerializer.Serialize(permissions))
			.Set(r => r.EditedUserId, userId)
			.UpdateAsync(token);

		await transaction.CommitAsync(token);
		await permissionCacheInvalidator.InvalidateOrganization(request.OrganizationId, CancellationToken.None);
	}
}
