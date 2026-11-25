using System.Globalization;
using Immediate.Cache.Shared;
using Immediate.Handlers.Shared;
using LinqToDB.Async;
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
public sealed partial class TenantGetUserPermissions(DbContext context, TimeProvider timeProvider)
{
	private async ValueTask<Response> HandleAsync(Query query, CancellationToken token)
	{
		var now = timeProvider.GetUtcNow();

		var apiKey = await context.AccessControl.ApiKeys
			.Where(k => k.ApiKeyId == query.UserId && k.User.IsActive)
			.Where(k => k.OwnerUser.IsActive && k.OwnerUser.ApiKey == null && k.ExpirationDateTime > now)
			.Select(
				k => new
				{
					k.OwnerUserId,
					PermissionsJson = k.TenantApiKey == null ? null : k.TenantApiKey.PermissionsJson,
				}
			)
			.FirstOrDefaultAsync(token);

		if (apiKey is { PermissionsJson: null })
			return new() { Permissions = [] };

		var memberUserId = apiKey?.OwnerUserId ?? query.UserId;

		var permissionSets = await context.TenantRoleUsers
			.Where(u => u.UserId == memberUserId && u.User.IsActive && u.User.ApiKey == null)
			.Where(u => u.TenantRole.TenantId == query.TenantId && u.TenantRole.Tenant.IsActive)
			.Select(u => u.TenantRole.PermissionsJson)
			.ToListAsync(token);

		var membershipPermissions = permissionSets
			.SelectMany(TenantManagement.ReadPermissions)
			.Distinct()
			.ToList();

		if (apiKey?.PermissionsJson is not { } grants || membershipPermissions.Count == 0)
			return new() { Permissions = membershipPermissions };

		return new()
		{
			Permissions = TenantManagement.ReadPermissions(grants)
				.Where(
					p => p is TenantPermission.None
						|| membershipPermissions.Contains(TenantPermission.Admin)
						|| membershipPermissions.Contains(p)
				)
				.Distinct()
				.ToList(),
		};
	}

	public sealed record Query
	{
		public required UserId UserId { get; init; }

		public required TenantId TenantId { get; init; }
	}

	public sealed record Response
	{
		public required IReadOnlyList<TenantPermission> Permissions { get; init; }
	}
}
