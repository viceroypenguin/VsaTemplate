using System.Data;
using System.Text.Json;
using Immediate.Apis.Shared;
using Immediate.Handlers.Shared;
using LinqToDB;
using LinqToDB.Async;
using Microsoft.AspNetCore.Mvc;
using VsaTemplate.Web.Database;
using VsaTemplate.Web.Features.AccessControl.Authorization;
using VsaTemplate.Web.Features.AccessControl.Services;
using VsaTemplate.Web.Features.Shared.Exceptions;
using VsaTemplate.Web.Features.Tenants.Authorization;
using VsaTemplate.Web.Features.Tenants.Models;
using VsaTemplate.Web.Features.Tenants.Services;

namespace VsaTemplate.Web.Features.Tenants.Endpoints;

[Handler]
[MapPut("/api/tenants/{tenantId}/roles/{roleId}")]
public sealed partial class SaveTenantRole(
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
		[FromRoute] public required TenantRoleId RoleId { get; init; }
		[FromBody] public required RequestBody RequestBody { get; init; }
	}

	public sealed record RequestBody
	{
		public required string Name { get; init; }
		public required IReadOnlyList<TenantPermission> Permissions { get; init; }
	}

	private async ValueTask HandleAsync([AsParameters] Request request, CancellationToken token)
	{
		var name = TenantManagement.ValidateName(request.RequestBody.Name);
		var permissions = TenantManagement.ValidatePermissions(request.RequestBody.Permissions);
		var actorPermissions = await currentTenantUserService.GetLoggedInUserPermissions(request.TenantId);
		TenantManagement.EnsureCanGrant(actorPermissions, permissions);
		var userId = await currentUserService.GetLoggedInUserId();

		await using var transaction = await context.BeginTransactionAsync(IsolationLevel.Serializable, token);

		var role = await context.TenantRoles
			.FirstOrDefaultAsync(r => r.TenantId == request.TenantId && r.TenantRoleId == request.RoleId, token)
			?? throw new NotFoundException(nameof(Database.Models.TenantRole));

		var oldPermissions = TenantManagement.ReadPermissions(role.PermissionsJson);
		TenantManagement.EnsureCanGrant(actorPermissions, oldPermissions);

		if (oldPermissions.Contains(TenantPermission.Admin) && !permissions.Contains(TenantPermission.Admin))
			await TenantManagement.EnsureAdministratorRemains(context, request.TenantId, request.RoleId, excludedUserId: null, token);

		await context.TenantRoles
			.Where(r => r.TenantId == request.TenantId && r.TenantRoleId == request.RoleId)
			.Set(r => r.Name, name)
			.Set(r => r.PermissionsJson, JsonSerializer.Serialize(permissions))
			.Set(r => r.EditedUserId, userId)
			.UpdateAsync(token);

		await transaction.CommitAsync(token);
		await cacheInvalidator.InvalidateTenant(request.TenantId, CancellationToken.None);
	}
}
