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
	public ValueTask<IReadOnlyList<Permission>> GetCurrentUserPermissions() =>
		GetCurrentUserId() switch
		{
			{ } userId => userPermissionCache.GetValue(new() { UserId = userId })
				.Transform(r => r.Permissions),

			_ => ValueTask.FromResult<IReadOnlyList<Permission>>([]),
		};

	public ValueTask<IReadOnlyList<Permission>> GetLoggedInUserPermissions() =>
		userPermissionCache.GetValue(new() { UserId = GetLoggedInUserId() })
			.Transform(r => r.Permissions);

	public UserId? GetCurrentUserId()
	{
		var user = httpContextAccessor.HttpContext?.User;

		if (user?.Identity?.IsAuthenticated != true)
			return null;

		var claim = user?.FindFirstValue("vsa-id") ?? "";

		if (!UserId.TryParse(claim, provider: null, out var userId))
			return null;

		return userId;
	}

	public UserId GetLoggedInUserId() =>
		GetCurrentUserId() ?? UnauthorizedException.ThrowUnauthorizedException<UserId>();

	// Memberships belong to a human owner; audit fields retain the authenticated
	// API-key principal so callers can distinguish actions performed by a key.
	public async ValueTask<UserId> GetMembershipUserId()
	{
		var actorId = GetLoggedInUserId();

		var apiKey = await context.AccessControl.ApiKeys
			.FirstOrDefaultAsync(k => k.ApiKeyId == actorId);

		if (apiKey is null)
			return actorId;

		if (!await context.AccessControl.Users.AnyAsync(u => u.UserId == apiKey.OwnerUserId && u.IsActive && u.ApiKey == null))
			UnauthorizedException.ThrowUnauthorizedException("The API key owner is unavailable.");

		return apiKey.OwnerUserId;
	}
}
