using System.Globalization;
using System.Text.Json;
using Immediate.Cache.Shared;
using Immediate.Handlers.Shared;
using LinqToDB.Async;
using VsaTemplate.Web.Database;
using VsaTemplate.Web.Features.AccessControl.Models;

namespace VsaTemplate.Web.Features.AccessControl.Services;

[CacheFor<GetUserPermissions>]
public sealed partial class UserPermissionCache
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
	private async ValueTask<Response> HandleAsync(
		Query query,
		CancellationToken token
	)
	{
		if (!await context.AccessControl.Users.AnyAsync(u => u.UserId == query.UserId && u.IsActive, token))
			return new() { Permissions = [] };

		var apiKey = await context.AccessControl.ApiKeys
			.FirstOrDefaultAsync(u => u.ApiKeyId == query.UserId, token);

		if (apiKey is null)
		{
			return new()
			{
				Permissions = (
						await context.AccessControl.RoleUsers
							.Where(u => u.UserId == query.UserId)
							.Select(u => u.Role)
							.Select(
								r => JsonSerializer.Deserialize<List<Permission>>(
									r.PermissionsJson,
									default(JsonSerializerOptions)
								)!
							)
							.ToListAsync(token)
					)
					.SelectMany(p => p)
					.DefaultIfEmpty(Permission.None)
					.Distinct()
					.ToList(),
			};
		}

		var parentPermissions = await userPermissionCache.GetValue(new() { UserId = apiKey.OwnerUserId }, token);

		if (parentPermissions.Permissions.Count == 0)
			return new() { Permissions = [] };

		return new()
		{
			Permissions = JsonSerializer.Deserialize<List<Permission>>(apiKey.PermissionsJson)!
				.Where(
					p => p == Permission.None
						|| parentPermissions.Permissions.Contains(Permission.Admin)
						|| parentPermissions.Permissions.Contains(p)
				)
				.DefaultIfEmpty(Permission.None)
				.Distinct()
				.ToList(),
		};
	}

	public sealed record Query
	{
		public required UserId UserId { get; init; }
	}

	public sealed record Response
	{
		public required IReadOnlyList<Permission> Permissions { get; init; }
	}
}
