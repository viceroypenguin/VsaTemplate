using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components.Authorization;

namespace VsaTemplate.Web.Features.Users.Services;

[RegisterScoped]
public sealed class CurrentUserService(
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
}
