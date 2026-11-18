using System.Security.Claims;
using Immediate.Injections.Shared;
using VsaTemplate.Web.Features.AccessControl.Models;
using VsaTemplate.Web.Features.Shared.Exceptions;
using VsaTemplate.Web.Features.Shared.Extensions;
using VsaTemplate.Web.Infrastructure.Authentication;

namespace VsaTemplate.Web.Features.AccessControl.Services;

[RegisterScoped]
public sealed class CurrentUserService(
	IHttpContextAccessor httpContextAccessor,
	UserPermissionCache userPermissionCache
)
{
	public async ValueTask<IReadOnlyList<Permission>> GetCurrentUserPermissions() =>
		GetCurrentUserId() switch
		{
			{ } userId => await userPermissionCache.GetValue(new() { UserId = userId })
				.Transform(r => r.Permissions),

			_ => [],
		};

	public async ValueTask<IReadOnlyList<Permission>> GetLoggedInUserPermissions() =>
		await userPermissionCache.GetValue(new() { UserId = GetLoggedInUserId() })
			.Transform(r => r.Permissions);

	public UserId? GetCurrentUserId()
	{
		var user = httpContextAccessor.HttpContext?.User;

		if (user?.Identity?.IsAuthenticated != true)
			return null;

		var claim = user.FindFirstValue("vsa-id") ?? "";

		if (!UserId.TryParse(claim, provider: null, out var userId))
			return null;

		return userId;
	}

	public UserId GetLoggedInUserId() =>
		GetCurrentUserId() ?? UnauthorizedException.ThrowUnauthorizedException<UserId>();

	public UserId GetLoggedInHumanUserId()
	{
		if (httpContextAccessor.HttpContext?.User.Identities.Any(identity =>
				identity.AuthenticationType == ApiKeyAuthenticationHandler.AuthenticationScheme
			) == true)
		{
			throw new UnauthorizedException("API keys cannot manage API keys.");
		}

		return GetLoggedInUserId();
	}

	// Memberships belong to a human owner; audit fields retain the authenticated
	// API-key principal so callers can distinguish actions performed by a key.
	public UserId GetMembershipUserId()
	{
		var actorId = GetLoggedInUserId();

		var user = httpContextAccessor.HttpContext?.User;
		var claim = user?.FindFirstValue("owner-id") ?? "";

		return UserId.TryParse(claim, provider: null, out var ownerId) switch
		{
			true => ownerId,
			false => actorId,
		};
	}
}
