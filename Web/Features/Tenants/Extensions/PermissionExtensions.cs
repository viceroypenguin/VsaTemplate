using VsaTemplate.Web.Features.Shared.Extensions;
using VsaTemplate.Web.Features.Tenants.Models;

namespace VsaTemplate.Web.Features.Tenants.Extensions;

public static class PermissionExtensions
{
	public static bool IsAdmin(this IReadOnlyList<TenantPermission> permissions) =>
		permissions.Contains(TenantPermission.Admin);

	public static async ValueTask<bool> IsAdmin(this ValueTask<IReadOnlyList<TenantPermission>> permissions) =>
		await permissions.Transform((IReadOnlyList<TenantPermission> p) => p.IsAdmin());

	public static bool HasPermission(this IReadOnlyList<TenantPermission> permissions, TenantPermission permission) =>
		permissions.Count > 0 && (permission is TenantPermission.None || permissions.IsAdmin() ||
			permissions.Contains(permission));

	public static async ValueTask<bool> HasPermission(
		this ValueTask<IReadOnlyList<TenantPermission>> permissions,
		TenantPermission permission
	) => await permissions.Transform((IReadOnlyList<TenantPermission> p) => p.HasPermission(permission));
}
