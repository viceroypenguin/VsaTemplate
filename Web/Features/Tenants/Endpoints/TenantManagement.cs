using System.Text.Json;
using LinqToDB.Async;
using VsaTemplate.Web.Database;
using VsaTemplate.Web.Features.AccessControl.Models;
using VsaTemplate.Web.Features.Shared.Exceptions;
using VsaTemplate.Web.Features.Tenants.Models;
using VsaTemplate.Web.Infrastructure.Exceptions;

namespace VsaTemplate.Web.Features.Tenants.Endpoints;

internal static class TenantManagement
{
	public static string ValidateName(string? name)
	{
		var result = name?.Trim();
		if (string.IsNullOrEmpty(result) || result.Length > 200)
			throw new TenantValidationException("Name must contain between 1 and 200 characters.");
		return result;
	}

	public static IReadOnlyList<TenantPermission> ValidatePermissions(IReadOnlyList<TenantPermission>? permissions)
	{
		if (permissions is not { Count: > 0 }
			|| permissions.Any(p => !Enum.IsDefined(p)))
			throw new TenantValidationException("Select at least one valid permission.");
		return permissions.Distinct().ToList();
	}

	public static IReadOnlyList<TenantPermission> ReadPermissions(string permissionsJson) =>
		JsonSerializer.Deserialize<List<TenantPermission>>(permissionsJson) ?? [];

	public static void EnsureCanGrant(IReadOnlyList<TenantPermission> actorPermissions, IReadOnlyList<TenantPermission> permissions)
	{
		if (!actorPermissions.Contains(TenantPermission.Admin)
			&& permissions.Any(p => p is TenantPermission.Admin || (p is not TenantPermission.None && !actorPermissions.Contains(p))))
			throw new UnauthorizedException("You cannot manage permissions beyond your own tenant permissions.");
	}

	public static async Task EnsureAdministratorRemains(
		DbContext context,
		TenantId tenantId,
		TenantRoleId? excludedRoleId,
		UserId? excludedUserId,
		CancellationToken token)
	{
		var roles = await context.TenantRoles
			.Where(r => r.TenantId == tenantId)
			.ToListAsync(token);
		var administratorRoleIds = roles
			.Where(r => (excludedRoleId is null || r.TenantRoleId != excludedRoleId.Value)
				&& ReadPermissions(r.PermissionsJson).Contains(TenantPermission.Admin))
			.Select(r => r.TenantRoleId)
			.ToList();
		var administrators = context.TenantRoleUsers
			.Where(u => administratorRoleIds.Contains(u.TenantRoleId))
			.Where(u => u.User.IsActive);
		if (excludedUserId is { } userId)
			administrators = administrators.Where(u => u.UserId != userId);
		if (!await administrators.AnyAsync(token))
			throw new ConflictException("A tenant must retain at least one active administrator.");
	}
}

internal sealed class TenantValidationException(string message) : VsaTemplateException(message, StatusCodes.Status400BadRequest);
