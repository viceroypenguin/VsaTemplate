using System.Text.Json;
using Immediate.Injections.Shared;
using LinqToDB.Async;
using VsaTemplate.Web.Database;
using VsaTemplate.Web.Features.AccessControl.Models;
using VsaTemplate.Web.Features.Organizations.Models;

namespace VsaTemplate.Web.Features.Organizations.Services;

[RegisterScoped]
public sealed class OrganizationPermissionReader(DbContext context, TimeProvider timeProvider)
{
	public async Task<IReadOnlyList<OrganizationPermission>> Read(
		UserId userId,
		OrganizationId organizationId,
		bool includeInactive,
		CancellationToken token
	)
	{
		var isActive = await context.Organizations
			.Where(o => o.OrganizationId == organizationId)
			.Select(o => (bool?)o.IsActive)
			.FirstOrDefaultAsync(token);

		if (isActive is null || (!isActive.Value && !includeInactive))
			return [];

		var now = timeProvider.GetUtcNow();

		var apiKey = await context.AccessControl.ApiKeys
			.Where(k => k.ApiKeyId == userId && k.User.IsActive)
			.Where(k => k.OwnerUser.IsActive && k.OwnerUser.ApiKey == null && k.ExpirationDateTime > now)
			.Select(k => new
				{
					k.OwnerUserId,
					PermissionsJson = k.OrganizationApiKey == null ? null : k.OrganizationApiKey.PermissionsJson,
				}
			)
			.FirstOrDefaultAsync(token);

		var memberUserId = apiKey?.OwnerUserId ?? userId;

		var roles = await context.OrganizationRoleUsers
			.Where(u => u.UserId == memberUserId && u.User.IsActive && u.User.ApiKey == null)
			.Where(u => u.OrganizationRole.OrganizationId == organizationId)
			.Select(u => u.OrganizationRole.PermissionsJson)
			.ToListAsync(token);

		var permissions = roles
			.SelectMany(json => JsonSerializer.Deserialize<OrganizationPermission[]>(json) ?? [])
			.Distinct()
			.ToArray();

		if (apiKey is null)
			return permissions;

		if (apiKey.PermissionsJson is null || permissions.Length == 0)
			return [];

		return (JsonSerializer.Deserialize<OrganizationPermission[]>(apiKey.PermissionsJson) ?? [])
			.Where(p => p is OrganizationPermission.None
				|| permissions.Contains(OrganizationPermission.Admin)
				|| permissions.Contains(p)
			)
			.Distinct()
			.ToArray();
	}
}
