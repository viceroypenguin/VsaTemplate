using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components.Authorization;
using VsaTemplate.Web.Features.Users.Models;
using VsaTemplate.Web.Infrastructure.Authorization;

namespace VsaTemplate.Web.Features.Users.Services;

[RegisterScoped]
internal sealed class CurrentUserService(
	IAuthorizationService authorizationService,
	IHttpContextAccessor httpContextAccessor,
	Task<AuthenticationState>? authenticationState = null
)
{
	public async ValueTask<ClaimsPrincipal?> GetCurrentUser()
	{
		if (httpContextAccessor.HttpContext is { User: { } user })
			return user;

		if (authenticationState is null)
			return null;

		var state = await authenticationState;
		return state.User;
	}

	public async ValueTask<bool> IsAuthorized(string policy)
	{
		if (await GetCurrentUser() is not { } user)
			return false;

		var auth = await authorizationService.AuthorizeAsync(user, policy);
		return auth.Succeeded;
	}

	public async ValueTask<UserId> GetCurrentUserId()
	{
		var user = await GetCurrentUser();

		var claim = user?.FindFirstValue(Claims.Id) ?? "";
		if (!UserId.TryParse(claim, out var userId))
			ThrowInvalidUserId(claim);

		return userId;
	}

	[StackTraceHidden]
	[DoesNotReturn]
	private static void ThrowInvalidUserId(string userId) =>
		throw new InvalidOperationException($"Unknown user id: {userId}");
}
