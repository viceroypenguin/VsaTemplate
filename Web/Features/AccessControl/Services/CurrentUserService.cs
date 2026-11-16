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

	public ValueTask<UserId?> GetCurrentUserId()
	{
		var user = httpContextAccessor.HttpContext?.User;

		if (user?.Identity?.IsAuthenticated != true)
			return ValueTask.FromResult<UserId?>(null);

		var claim = user?.FindFirstValue("vsa-id") ?? "";

		if (!UserId.TryParse(claim, provider: null, out var userId))
			return ValueTask.FromResult<UserId?>(null);

		return ValueTask.FromResult<UserId?>(userId);
	}

	public async ValueTask<UserId> GetLoggedInUserId() =>
		await GetCurrentUserId() ?? UnauthorizedException.ThrowUnauthorizedException<UserId>();

	// Memberships belong to a human owner; audit fields retain the authenticated
	// API-key principal so callers can distinguish actions performed by a key.
	public async ValueTask<UserId> GetMembershipUserId()
	{
		var actorId = await GetLoggedInUserId();

		var apiKey = await context.AccessControl.ApiKeys
			.FirstOrDefaultAsync(k => k.ApiKeyId == actorId);

		if (apiKey is null)
			return actorId;

		if (!await context.AccessControl.Users
			.AnyAsync(u => u.UserId == apiKey.OwnerUserId && u.IsActive && u.ApiKey == null))
			UnauthorizedException.ThrowUnauthorizedException("The API key owner is unavailable.");

		return apiKey.OwnerUserId;
	}
}
