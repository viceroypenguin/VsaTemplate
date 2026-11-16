using VsaTemplate.Web.Features.Organizations.Models;
using VsaTemplate.Web.Features.Shared.Extensions;

namespace VsaTemplate.Web.Features.Organizations.Extensions;

public static class PermissionExtensions
{
	public static bool IsAdmin(this IReadOnlyList<OrganizationPermission> permissions) =>
		permissions.Contains(OrganizationPermission.Admin);

	public static async ValueTask<bool> IsAdmin(this ValueTask<IReadOnlyList<OrganizationPermission>> permissions) =>
		await permissions.Transform((IReadOnlyList<OrganizationPermission> p) => IsAdmin(p));

	public static bool HasPermission(this IReadOnlyList<OrganizationPermission> permissions, OrganizationPermission permission) =>
		permissions.Count > 0 && (permission is OrganizationPermission.None || permissions.IsAdmin() || permissions.Contains(permission));

	public static async ValueTask<bool> HasPermission(
		this ValueTask<IReadOnlyList<OrganizationPermission>> permissions,
		OrganizationPermission permission
	) => await permissions.Transform((IReadOnlyList<OrganizationPermission> p) => HasPermission(p, permission));
}
