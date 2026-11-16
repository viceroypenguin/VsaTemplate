using System.Security.Claims;
using Immediate.Injections.Shared;
using LinqToDB.Async;
using VsaTemplate.Web.Database;
using VsaTemplate.Web.Features.AccessControl.Models;
using VsaTemplate.Web.Features.Shared.Exceptions;
using VsaTemplate.Web.Features.Shared.Extensions;

namespace VsaTemplate.Web.Features.AccessControl.Services;

[RegisterScoped]
public sealed class CurrentUserService(
	IHttpContextAccessor httpContextAccessor,
	DbContext context,
	UserPermissionCache userPermissionCache
)
{
	public async ValueTask<IReadOnlyList<Permission>> GetCurrentUserPermissions() =>
		await GetCurrentUserId() switch
		{
			{ } userId => await userPermissionCache.GetValue(new() { UserId = userId })
				.Transform(r => r.Permissions),

			_ => [],
		};

	public async ValueTask<IReadOnlyList<Permission>> GetLoggedInUserPermissions() =>
		await userPermissionCache.GetValue(new() { UserId = await GetLoggedInUserId() })
			.Transform(r => r.Permissions);

	public async ValueTask<UserId?> GetCurrentUserId()
	{
		var user = httpContextAccessor.HttpContext?.User;
		if (user?.Identity?.IsAuthenticated != true)
			return null;

		var claim = user?.FindFirstValue("vsa-id") ?? "";
		if (!UserId.TryParse(claim, provider: null, out var userId))
			return null;

		return await context.AccessControl.Users.AnyAsync(u => u.UserId == userId && u.IsActive)
			? userId
			: null;
	}

	public async ValueTask<UserId> GetLoggedInUserId() =>
		await GetCurrentUserId() ?? UnauthorizedException.ThrowUnauthorizedException<UserId>();

	// Memberships belong to a human owner; audit fields retain the authenticated
	// API-key principal so callers can distinguish actions performed by a key.
	public async ValueTask<UserId> GetMembershipUserId()
	{
		var actorId = await GetLoggedInUserId();
		var apiKey = await context.AccessControl.ApiKeys.FirstOrDefaultAsync(k => k.ApiKeyId == actorId);
		if (apiKey is null)
			return actorId;
		if (!await context.AccessControl.Users.AnyAsync(u => u.UserId == apiKey.OwnerUserId && u.IsActive && u.ApiKey == null))
			UnauthorizedException.ThrowUnauthorizedException("The API key owner is unavailable.");
		return apiKey.OwnerUserId;
	}
}

public static class PermissionExtensions
{
	public static bool IsAdmin(this IReadOnlyList<Permission> permissions) =>
		permissions.Contains(Permission.Admin);

	public static async ValueTask<bool> IsAdmin(this ValueTask<IReadOnlyList<Permission>> permissions) =>
		await permissions.Transform((IReadOnlyList<Permission> p) => IsAdmin(p));

	public static bool HasPermission(this IReadOnlyList<Permission> permissions, Permission permission) =>
		permissions.IsAdmin() || permissions.Contains(permission);

	public static async ValueTask<bool> HasPermission(
		this ValueTask<IReadOnlyList<Permission>> permissions,
		Permission permission
	) => await permissions.Transform((IReadOnlyList<Permission> p) => HasPermission(p, permission));
}
