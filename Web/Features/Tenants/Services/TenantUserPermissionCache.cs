using System.Globalization;
using System.Text.Json;
using Immediate.Cache.Shared;
using Immediate.Handlers.Shared;
using LinqToDB.Async;
using SuperLinq;
using VsaTemplate.Web.Database;
using VsaTemplate.Web.Features.AccessControl.Models;
using VsaTemplate.Web.Features.Tenants.Models;

namespace VsaTemplate.Web.Features.Tenants.Services;

[CacheFor<TenantGetUserPermissions>]
public sealed partial class TenantUserPermissionCache
{
	protected override string TransformKey(TenantGetUserPermissions.Query request) =>
		string.Create(CultureInfo.InvariantCulture, $"TenantUserPermissionCache-{request.UserId}-{request.TenantId}");
}

[Handler]
public sealed partial class TenantGetUserPermissions(
	DbContext context,
	TenantUserPermissionCache tenantUserPermissionCache
)
{
	public sealed record Query
	{
		public required UserId UserId { get; init; }
		public required TenantId TenantId { get; init; }
	}

	public sealed record Response
	{
		public required IReadOnlyList<TenantPermission> Permissions { get; init; }
	}

	private async ValueTask<Response> HandleAsync(
		Query query,
		CancellationToken token
	)
	{
		var apiKey = await context.AccessControl.ApiKeys
			.Where(u => u.ApiKeyId == query.UserId)
			.Select(u => new
			{
				u.OwnerUserId,
				PermissionsJson = u.TenantApiKey == null ? null : u.TenantApiKey.PermissionsJson,
			})
			.FirstOrDefaultAsync(token);

		if (apiKey is null)
		{
			return new()
			{
				Permissions = (
					await context.TenantRoleUsers
						.Where(u => u.UserId == query.UserId)
						.Select(u => u.TenantRole)
						.Where(r => r.TenantId == query.TenantId)
						.Select(r => JsonSerializer.Deserialize<List<TenantPermission>>(r.PermissionsJson, default(JsonSerializerOptions))!)
						.ToListAsync(token)
				)
					.SelectMany(SuperEnumerable.Identity)
					.Distinct()
					.ToList(),
			};
		}

		var parentPermissions = await tenantUserPermissionCache.GetValue(
			new() { UserId = apiKey.OwnerUserId, TenantId = query.TenantId },
			token
		);

		return new()
		{
			Permissions = apiKey.PermissionsJson is null || parentPermissions.Permissions.Count == 0
				? []
				: JsonSerializer.Deserialize<List<TenantPermission>>(apiKey.PermissionsJson, default(JsonSerializerOptions))!
					.Where(p => p == TenantPermission.None || parentPermissions.Permissions.Contains(TenantPermission.Admin) || parentPermissions.Permissions.Contains(p))
					.Distinct()
					.ToList(),
		};
	}
}
