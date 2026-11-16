using Immediate.Injections.Shared;
using VsaTemplate.Web.Features.AccessControl.Services;
using VsaTemplate.Web.Features.Shared.Extensions;
using VsaTemplate.Web.Features.Tenants.Models;

namespace VsaTemplate.Web.Features.Tenants.Services;

[RegisterScoped]
public sealed class CurrentTenantUserService(
	CurrentUserService currentUserService,
	TenantUserPermissionCache tenantUserPermissionCache
)
{
	public async ValueTask<IReadOnlyList<TenantPermission>> GetCurrentUserPermissions(TenantId tenantId) =>
		currentUserService.GetCurrentUserId() switch
		{
			{ } userId => await tenantUserPermissionCache.GetValue(new() { UserId = userId, TenantId = tenantId })
				.Transform(r => r.Permissions),

			_ => [],
		};

	public async ValueTask<IReadOnlyList<TenantPermission>> GetLoggedInUserPermissions(TenantId tenantId) =>
		await tenantUserPermissionCache.GetValue(new() { UserId = currentUserService.GetLoggedInUserId(), TenantId = tenantId })
			.Transform(r => r.Permissions);
}
