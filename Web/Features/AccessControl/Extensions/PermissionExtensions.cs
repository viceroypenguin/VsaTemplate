using VsaTemplate.Web.Features.AccessControl.Models;
using VsaTemplate.Web.Features.Shared.Extensions;

namespace VsaTemplate.Web.Features.AccessControl.Extensions;

public static class PermissionExtensions
{
	public static bool IsAdmin(this IReadOnlyList<Permission> permissions) =>
		permissions.Contains(Permission.Admin);

	public static async ValueTask<bool> IsAdmin(this ValueTask<IReadOnlyList<Permission>> permissions) =>
		await permissions.Transform((IReadOnlyList<Permission> p) => p.IsAdmin());

	public static bool HasPermission(this IReadOnlyList<Permission> permissions, Permission permission) =>
		permissions.IsAdmin() || permissions.Contains(permission);

	public static async ValueTask<bool> HasPermission(
		this ValueTask<IReadOnlyList<Permission>> permissions,
		Permission permission
	) => await permissions.Transform((IReadOnlyList<Permission> p) => p.HasPermission(permission));
}
