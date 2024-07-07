using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using VsaTemplate.Api.Features.Users.Models;
using VsaTemplate.Api.Infrastructure.Authorization;

namespace VsaTemplate.Api.Features.Users.Services;

[RegisterSingleton]
public sealed class CurrentUserService(
	IAuthorizationService authorizationService,
	IHttpContextAccessor httpContextAccessor
)
{
	private ClaimsPrincipal? GetCurrentUser() =>
		httpContextAccessor.HttpContext?.User;

	public async ValueTask<bool> IsAuthorized(string policy)
	{
		if (GetCurrentUser() is not { } user)
			return false;

		var auth = await authorizationService.AuthorizeAsync(user, policy);
		return auth.Succeeded;
	}

	public ValueTask<UserId> GetCurrentUserId()
	{
		var user = GetCurrentUser();

		var claim = user?.FindFirstValue(Claims.Id) ?? "";
		if (!UserId.TryParse(claim, out var userId))
			ThrowInvalidUserId(claim);

		return ValueTask.FromResult(userId);
	}

	[StackTraceHidden]
	[DoesNotReturn]
	private static void ThrowInvalidUserId(string userId) =>
		throw new InvalidOperationException($"Unknown user id: {userId}");
}
