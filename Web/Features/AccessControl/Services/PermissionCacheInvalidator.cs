using Immediate.Injections.Shared;
using LinqToDB.Async;
using VsaTemplate.Web.Database;
using VsaTemplate.Web.Features.AccessControl.Models;
using VsaTemplate.Web.Features.Organizations.Models;
using VsaTemplate.Web.Features.Organizations.Services;
using VsaTemplate.Web.Features.Tenants.Models;
using VsaTemplate.Web.Features.Tenants.Services;

namespace VsaTemplate.Web.Features.AccessControl.Services;

// After committing a mutation, revocation must finish, so no accepted cancellation token.
[RegisterScoped]
public sealed class PermissionCacheInvalidator(
	DbContext context,
	UserPermissionCache userPermissions,
	TenantUserPermissionCache tenantPermissions,
	OrganizationUserPermissionCache organizationPermissions,
	GetOrganizationsByUserCache organizations
)
{
	public async Task InvalidateTenant(TenantId tenantId)
	{
		var userIds = await context.TenantRoleUsers
			.Where(u => u.TenantRole.TenantId == tenantId)
			.Select(u => u.UserId)
			.Union(
				context.OrganizationRoleUsers
					.Where(u => u.OrganizationRole.Organization.TenantId == tenantId)
					.Select(u => u.UserId)
			)
			.ToListAsync(CancellationToken.None);

		foreach (var userId in userIds)
			await InvalidateUser(userId);
	}

	public async Task InvalidateOrganization(OrganizationId organizationId)
	{
		var userIds = await context.OrganizationRoleUsers
			.Where(u => u.OrganizationRole.OrganizationId == organizationId)
			.Select(u => u.UserId)
			.Distinct()
			.ToListAsync(CancellationToken.None);

		foreach (var userId in userIds)
			await InvalidateUser(userId);
	}

	public async Task InvalidateUser(UserId userId)
	{
		// Include delegated API keys and revoked memberships: their old cache entries
		// must be removed even when the membership rows no longer exist.

		var userIds = await context.AccessControl.ApiKeys
			.Where(k => k.OwnerUserId == userId)
			.Select(k => k.ApiKeyId)
			.ToListAsync(CancellationToken.None);

		userIds.Add(userId);

		var tenantIds = await context.Tenants
			.Select(t => t.TenantId)
			.ToListAsync(CancellationToken.None);

		var organizationIds = await context.Organizations
			.Select(o => o.OrganizationId)
			.ToListAsync(CancellationToken.None);

		foreach (var id in userIds)
		{
			userPermissions.RemoveValue(new() { UserId = id });
			organizations.RemoveValue(new() { UserId = id });

			foreach (var tenantId in tenantIds)
				tenantPermissions.RemoveValue(new() { UserId = id, TenantId = tenantId });

			foreach (var organizationId in organizationIds)
				organizationPermissions.RemoveValue(new() { UserId = id, OrganizationId = organizationId });
		}
	}
}
