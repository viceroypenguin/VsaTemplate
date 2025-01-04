using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components.Authorization;
using VsaTemplate.Web.Features.Users.Models;
using VsaTemplate.Web.Infrastructure.Authorization;

namespace VsaTemplate.Web.Features.Users.Services;

[RegisterScoped]
public sealed class CurrentUserService(
	IAuthorizationService authorizationService,
	IHttpContextAccessor httpContextAccessor,
	AuthenticationStateProvider authenticationStateProvider,
	UserRolesCache userRolesCache
)
{
	private async ValueTask<ClaimsPrincipal?> GetCurrentUser()
	{
		if (httpContextAccessor.HttpContext is { User: { } user })
			return user;

		var authenticationState = await authenticationStateProvider
			.GetAuthenticationStateAsync();

		return authenticationState?.User;
	}

	public async ValueTask<bool> IsAuthorized(string policy)
	{
		if (await GetCurrentUser() is not { } user)
			return false;

		user = await TransformAsync(user);

		var auth = await authorizationService.AuthorizeAsync(user, policy);
		return auth.Succeeded;
	}

	public async ValueTask<UserId> GetCurrentUserId()
	{
		var user = await GetCurrentUser();

		var claim = user?.FindFirstValue(Claims.Id) ?? "";
		if (!UserId.TryParse(claim, provider: null, out var userId))
			ThrowInvalidUserId(claim);

		return userId;
	}

	[StackTraceHidden]
	[DoesNotReturn]
	private static void ThrowInvalidUserId(string userId) =>
		throw new InvalidOperationException($"Unknown user id: {userId}");

	private async Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal)
	{
		var claim = principal.FindFirstValue(Claims.Id) ?? "";
		if (!UserId.TryParse(claim, provider: null, out var userId))
			ThrowInvalidUserId(claim);

		var roles = await userRolesCache.GetValue(new() { UserId = userId, });

		return new ClaimsPrincipal(
			new ClaimsIdentity(
				principal.Claims
					.Where(c => !string.Equals(c.Type, ClaimTypes.Role, StringComparison.Ordinal))
					.Concat(
						roles
							.Select(r => new Claim(ClaimTypes.Role, r))
					)
			)
		);
	}
}
