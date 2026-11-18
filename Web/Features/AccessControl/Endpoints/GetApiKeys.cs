using System.Text.Json;
using Immediate.Apis.Shared;
using Immediate.Handlers.Shared;
using LinqToDB.Async;
using VsaTemplate.Web.Database;
using VsaTemplate.Web.Features.AccessControl.Authorization;
using VsaTemplate.Web.Features.AccessControl.Extensions;
using VsaTemplate.Web.Features.AccessControl.Models;
using VsaTemplate.Web.Features.AccessControl.Services;

namespace VsaTemplate.Web.Features.AccessControl.Endpoints;

[Handler]
[MapGet("/api/users/apikey")]
public sealed partial class GetApiKeys(
	CurrentUserService currentUserService,
	DbContext context,
	TimeProvider timeProvider
)
{
	public sealed record Request : IAuthorizedRequest;

	public sealed record ApiKey
	{
		public required UserId ApiKeyId { get; init; }

		public required string? Note { get; init; }

		public required IReadOnlyList<Permission> Permissions { get; init; }

		public required string KeySuffix { get; init; }
	}

	public sealed record Response
	{
		public required IReadOnlyList<ApiKey> ApiKeys { get; init; }

		public required bool CanCreateAdminKey { get; init; }
	}

	private async ValueTask<Response> HandleAsync(Request _, CancellationToken token)
	{
		var userId = currentUserService.GetLoggedInHumanUserId();
		var now = timeProvider.GetUtcNow();

		var keys = await context.AccessControl.ApiKeys
			.Where(k => k.OwnerUserId == userId && k.User.IsActive && k.ExpirationDateTime > now)
			.OrderByDescending(k => k.CreatedDateTime)
			.ThenByDescending(k => k.ApiKeyId)
			.Select(k => new
			{
				k.ApiKeyId,
				k.Note,
				k.PermissionsJson,
				KeySuffix = k.User.EmailAddress.Length > 3
					? k.User.EmailAddress.Substring(k.User.EmailAddress.Length - 3)
					: "",
			})
			.ToListAsync(token);

		return new()
		{
			ApiKeys = keys
				.Select(k => new ApiKey
				{
					ApiKeyId = k.ApiKeyId,
					Note = k.Note,
					Permissions = JsonSerializer.Deserialize<Permission[]>(k.PermissionsJson)!,
					KeySuffix = k.KeySuffix,
				})
				.ToList(),
			CanCreateAdminKey = await currentUserService.GetLoggedInUserPermissions().IsAdmin(),
		};
	}
}
