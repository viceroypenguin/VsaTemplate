using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;
using VsaTemplate.Web.Features.AccessControl.Models;
using VsaTemplate.Web.Features.Shared.Extensions;

namespace VsaTemplate.Web.Features.AccessControl.Services;

[RegisterScoped]
public sealed class CurrentUserService(
	IHttpContextAccessor httpContextAccessor,
	AuthenticationStateProvider authenticationStateProvider,
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
		var user = await GetCurrentUser();

		var claim = user?.FindFirstValue("vsa-id") ?? "";
		if (!UserId.TryParse(claim, provider: null, out var userId))
			return null;

		return userId;
	}

	public async ValueTask<UserId> GetLoggedInUserId()
	{
		var user = await GetCurrentUser();

		var claim = user?.FindFirstValue("vsa-id") ?? "";
		if (!UserId.TryParse(claim, provider: null, out var userId))
			ThrowInvalidUserId(claim);

		return userId;
	}

	private async ValueTask<ClaimsPrincipal?> GetCurrentUser()
	{
		if (httpContextAccessor.HttpContext is { User: { } user })
			return user;

		var authenticationState = await authenticationStateProvider
			.GetAuthenticationStateAsync();

		return authenticationState?.User;
	}

	[StackTraceHidden]
	[DoesNotReturn]
	private static void ThrowInvalidUserId(string userId) =>
		throw new InvalidOperationException($"Unknown user id: `{userId}`");
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
