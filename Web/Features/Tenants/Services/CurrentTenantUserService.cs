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
		await currentUserService.GetCurrentUserId() switch
		{
			{ } userId => await tenantUserPermissionCache.GetValue(new() { UserId = userId, TenantId = tenantId })
				.Transform(r => r.Permissions),

			_ => [],
		};

	public async ValueTask<IReadOnlyList<TenantPermission>> GetLoggedInUserPermissions(TenantId tenantId) =>
		await tenantUserPermissionCache.GetValue(new() { UserId = await currentUserService.GetLoggedInUserId(), TenantId = tenantId })
			.Transform(r => r.Permissions);
}

public static class PermissionExtensions
{
	public static bool IsAdmin(this IReadOnlyList<TenantPermission> permissions) =>
		permissions.Contains(TenantPermission.Admin);

	public static async ValueTask<bool> IsAdmin(this ValueTask<IReadOnlyList<TenantPermission>> permissions) =>
		await permissions.Transform((IReadOnlyList<TenantPermission> p) => IsAdmin(p));

	public static bool HasPermission(this IReadOnlyList<TenantPermission> permissions, TenantPermission permission) =>
		permissions.IsAdmin() || permissions.Contains(permission);

	public static async ValueTask<bool> HasPermission(
		this ValueTask<IReadOnlyList<TenantPermission>> permissions,
		TenantPermission permission
	) => await permissions.Transform((IReadOnlyList<TenantPermission> p) => HasPermission(p, permission));
}
