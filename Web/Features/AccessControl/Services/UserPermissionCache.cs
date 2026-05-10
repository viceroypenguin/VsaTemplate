using System.Globalization;
using System.Text.Json;
using Immediate.Cache;
using Immediate.Handlers.Shared;
using LinqToDB;
using Microsoft.Extensions.Caching.Memory;
using SuperLinq;
using VsaTemplate.Web.Database;
using VsaTemplate.Web.Features.AccessControl.Models;

namespace VsaTemplate.Web.Features.AccessControl.Services;

[RegisterSingleton]
public sealed class UserPermissionCache(
	IMemoryCache memoryCache,
	Owned<IHandler<GetUserPermissions.Query, GetUserPermissions.Response>> ownedGetUserQueries
)
	: ApplicationCacheBase<
		GetUserPermissions.Query,
		GetUserPermissions.Response
	>(memoryCache, ownedGetUserQueries)
{
	protected override string TransformKey(GetUserPermissions.Query request) =>
		string.Create(CultureInfo.InvariantCulture, $"UserPermissionCache-{request.UserId}");
}

[Handler]
public sealed partial class GetUserPermissions(
	DbContext context,
	UserPermissionCache userPermissionCache
)
{
	public sealed record Query
	{
		public required UserId UserId { get; init; }
	}

	public sealed record Response
	{
		public required IReadOnlyList<Permission> Permissions { get; init; }
	}

	private async ValueTask<Response> HandleAsync(
		Query query,
		CancellationToken token
	)
	{
		var apiKey = await context.ApiKeys
			.FirstOrDefaultAsync(u => u.ApiKeyId == query.UserId, token);

		if (apiKey == null)
		{
			return new()
			{
				Permissions = (
					await context.RoleUsers
						.Where(u => u.UserId == query.UserId)
						.Select(u => u.Role)
						.Select(r => JsonSerializer.Deserialize<List<Permission>>(r.PermissionsJson, default(JsonSerializerOptions))!)
						.ToListAsync(token)
				)
					.SelectMany(SuperEnumerable.Identity)
					.DefaultIfEmpty(Permission.None)
					.Distinct()
					.ToList(),
			};
		}

		var parentPermissions = await userPermissionCache.GetValue(new() { UserId = apiKey.OwnerUserId }, token);
		return new()
		{
			Permissions = JsonSerializer.Deserialize<List<Permission>>(apiKey.PermissionsJson, default(JsonSerializerOptions))!
				.Intersect(parentPermissions.Permissions)
				.ToList(),
		};
	}
}
