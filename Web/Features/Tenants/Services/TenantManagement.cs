using System.Text.Json;
using LinqToDB.Async;
using VsaTemplate.Web.Database;
using VsaTemplate.Web.Features.AccessControl.Models;
using VsaTemplate.Web.Features.Shared.Exceptions;
using VsaTemplate.Web.Features.Tenants.Models;

namespace VsaTemplate.Web.Features.Tenants.Services;

internal static class TenantManagement
{
	public static IReadOnlyList<TenantPermission> ReadPermissions(string permissionsJson) =>
		JsonSerializer.Deserialize<List<TenantPermission>>(permissionsJson) ?? [];

	public static void EnsureCanGrant(
		IReadOnlyList<TenantPermission> actorPermissions,
		IReadOnlyList<TenantPermission> permissions
	)
	{
		if (!actorPermissions.Contains(TenantPermission.Admin)
			&& permissions.Any(p =>
				p is TenantPermission.Admin || (p is not TenantPermission.None && !actorPermissions.Contains(p))
			))
			throw new UnauthorizedException("You cannot manage permissions beyond your own tenant permissions.");
	}

	public static async Task EnsureAdministratorRemains(
		DbContext context,
		TenantId tenantId,
		TenantRoleId? excludedRoleId,
		UserId? excludedUserId,
		CancellationToken token
	)
	{
		var roles = await context.TenantRoles
			.Where(r => r.TenantId == tenantId)
			.ToListAsync(token);

		var administratorRoleIds = roles
			.Where(r => (excludedRoleId is null || r.TenantRoleId != excludedRoleId.Value)
				&& ReadPermissions(r.PermissionsJson).Contains(TenantPermission.Admin)
			)
			.Select(r => r.TenantRoleId)
			.ToList();

		var administrators = context.TenantRoleUsers
			.Where(u => administratorRoleIds.Contains(u.TenantRoleId))
			.Where(u => u.User.IsActive);

		if (excludedUserId is { } userId)
		{
			administrators = administrators
				.Where(u => u.UserId != userId);
		}

		if (!await administrators.AnyAsync(token))
			throw new ConflictException("A tenant must retain at least one active administrator.");
	}
}
