using Immediate.Injections.Shared;
using VsaTemplate.Web.Features.AccessControl.Services;
using VsaTemplate.Web.Features.Shared.Extensions;
using VsaTemplate.Web.Features.Organizations.Models;

namespace VsaTemplate.Web.Features.Organizations.Services;

[RegisterScoped]
public sealed class CurrentOrganizationUserService(
	CurrentUserService currentUserService,
	OrganizationUserPermissionCache organizationUserPermissionCache
)
{
	public async ValueTask<IReadOnlyList<OrganizationPermission>> GetCurrentUserPermissions(OrganizationId organizationId) =>
		await currentUserService.GetCurrentUserId() switch
		{
			{ } userId => await organizationUserPermissionCache.GetValue(new() { UserId = userId, OrganizationId = organizationId })
				.Transform(r => r.Permissions),

			_ => [],
		};

	public async ValueTask<IReadOnlyList<OrganizationPermission>> GetLoggedInUserPermissions(OrganizationId organizationId) =>
		await organizationUserPermissionCache.GetValue(new() { UserId = await currentUserService.GetLoggedInUserId(), OrganizationId = organizationId })
			.Transform(r => r.Permissions);
}

public static class PermissionExtensions
{
	public static bool IsAdmin(this IReadOnlyList<OrganizationPermission> permissions) =>
		permissions.Contains(OrganizationPermission.Admin);

	public static async ValueTask<bool> IsAdmin(this ValueTask<IReadOnlyList<OrganizationPermission>> permissions) =>
		await permissions.Transform((IReadOnlyList<OrganizationPermission> p) => IsAdmin(p));

	public static bool HasPermission(this IReadOnlyList<OrganizationPermission> permissions, OrganizationPermission permission) =>
		permissions is not [OrganizationPermission.None] && (permission is OrganizationPermission.None || permissions.IsAdmin() || permissions.Contains(permission));

	public static async ValueTask<bool> HasPermission(
		this ValueTask<IReadOnlyList<OrganizationPermission>> permissions,
		OrganizationPermission permission
	) => await permissions.Transform((IReadOnlyList<OrganizationPermission> p) => HasPermission(p, permission));
}
