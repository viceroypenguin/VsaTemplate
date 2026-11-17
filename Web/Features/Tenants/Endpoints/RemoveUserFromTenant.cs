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
using VsaTemplate.Web.Features.Tenants.Authorization;
using VsaTemplate.Web.Features.Tenants.Models;
using VsaTemplate.Web.Features.Tenants.Services;

namespace VsaTemplate.Web.Features.Tenants.Endpoints;

[Handler]
[MapDelete("/api/tenants/{tenantId}/users/{userId}")]
public sealed partial class RemoveUserFromTenant(
	CurrentUserService currentUserService,
	CurrentTenantUserService currentTenantUserService,
	DbContext context,
	PermissionCacheInvalidator cacheInvalidator
)
{
	public sealed record Request : IAuthorizedRequest, ITenantRequest
	{
		public static TenantPermission TenantPermission => TenantPermission.UserManagement;

		[FromRoute]
		public required TenantId TenantId { get; init; }

		[FromRoute]
		public required UserId UserId { get; init; }
	}

	private async ValueTask HandleAsync([AsParameters] Request request, CancellationToken token)
	{
		var actorId = currentUserService.GetLoggedInUserId();
		var actorPermissions = await currentTenantUserService.GetLoggedInUserPermissions(request.TenantId);
		await using var transaction = await context.BeginTransactionAsync(IsolationLevel.Serializable, token);

		var permissionsJson = await context.TenantRoleUsers
			.Where(u => u.UserId == request.UserId && u.TenantRole.TenantId == request.TenantId)
			.Select(u => u.TenantRole.PermissionsJson)
			.ToListAsync(token);

		if (permissionsJson.Count == 0)
			return;

		var permissions = permissionsJson
			.SelectMany(TenantManagement.ReadPermissions)
			.Distinct()
			.ToList();

		TenantManagement.EnsureCanGrant(actorPermissions, permissions);

		if (permissions.Contains(TenantPermission.Admin))
			await TenantManagement.EnsureAdministratorRemains(context, request.TenantId, excludedRoleId: null, request.UserId, token);

		var memberships = context.TenantRoleUsers
			.Where(u => u.UserId == request.UserId && u.TenantRole.TenantId == request.TenantId);

		await memberships
			.Set(u => u.EditedUserId, actorId)
			.UpdateAsync(token);

		await memberships
			.DeleteAsync(token);

		await transaction.CommitAsync(token);
		await cacheInvalidator.InvalidateUser(request.UserId, CancellationToken.None);
		await cacheInvalidator.InvalidateTenant(request.TenantId, CancellationToken.None);
	}
}
