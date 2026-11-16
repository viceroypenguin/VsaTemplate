using System.Text.Json;
using Immediate.Injections.Shared;
using LinqToDB.Async;
using VsaTemplate.Web.Database;
using VsaTemplate.Web.Features.AccessControl.Models;
using VsaTemplate.Web.Features.Tenants.Models;

namespace VsaTemplate.Web.Features.Tenants.Services;

[RegisterScoped]
public sealed class TenantPermissionReader(DbContext context, TimeProvider timeProvider)
{
	public async Task<IReadOnlyList<TenantPermission>> Read(
		UserId userId,
		TenantId tenantId,
		bool includeInactive,
		CancellationToken token
	)
	{

		if (!await context.AccessControl.Users
			.AnyAsync(u => u.UserId == userId && u.IsActive, token)
			|| !await context.Tenants
				.AnyAsync(t => t.TenantId == tenantId && (includeInactive || t.IsActive), token))
		{
			return [];
		}

		var apiKey = await context.AccessControl.ApiKeys
			.Where(k => k.ApiKeyId == userId)
			.Select(k => new
			{
				k.OwnerUserId,
				k.ExpirationDateTime,
				OwnerIsActive = k.OwnerUser.IsActive,
				OwnerIsApiKey = k.OwnerUser.ApiKey != null,
				PermissionsJson = k.TenantApiKey == null ? null : k.TenantApiKey.PermissionsJson,
			})
			.FirstOrDefaultAsync(token);

		if (apiKey is null)
		{

			var permissionSets = await context.TenantRoleUsers
				.Where(u => u.UserId == userId && u.TenantRole.TenantId == tenantId)
				.Select(u => u.TenantRole.PermissionsJson)
				.ToListAsync(token);

			return permissionSets
				.SelectMany(p => JsonSerializer.Deserialize<List<TenantPermission>>(p) ?? [])
				.Distinct()
				.ToList();

		}

		if (apiKey is not { OwnerIsActive: true, OwnerIsApiKey: false, PermissionsJson: { } grants }
			|| apiKey.ExpirationDateTime <= timeProvider.GetUtcNow())
		{
			return [];
		}

		var ownerPermissions = await Read(apiKey.OwnerUserId, tenantId, includeInactive, token);

		if (ownerPermissions.Count == 0)
			return [];

		return (JsonSerializer.Deserialize<List<TenantPermission>>(grants) ?? [])
			.Where(p => p == TenantPermission.None || ownerPermissions.Contains(TenantPermission.Admin) || ownerPermissions.Contains(p))
			.Distinct()
			.ToList();

	}
}
