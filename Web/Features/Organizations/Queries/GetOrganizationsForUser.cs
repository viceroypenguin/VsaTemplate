using Immediate.Handlers.Shared;
using LinqToDB.Async;
using VsaTemplate.Web.Database;
using VsaTemplate.Web.Features.AccessControl.Models;
using VsaTemplate.Web.Features.Organizations.Models;
using VsaTemplate.Web.Features.Organizations.Services;
using VsaTemplate.Web.Features.Tenants.Models;

namespace VsaTemplate.Web.Features.Organizations.Queries;

[Handler]
public sealed partial class GetOrganizationsForUser(DbContext context, TimeProvider timeProvider)
{
	private async ValueTask<IReadOnlyList<Organization>> HandleAsync(Query query, CancellationToken token)
	{
		var now = timeProvider.GetUtcNow();

		var apiKey = await context.AccessControl.ApiKeys
			.Where(k => k.ApiKeyId == query.UserId && k.User.IsActive)
			.Where(k => k.OwnerUser.IsActive && k.OwnerUser.ApiKey == null && k.ExpirationDateTime > now)
			.Select(
				k => new
				{
					k.OwnerUserId,
					PermissionsJson = k.OrganizationApiKey == null ? null : k.OrganizationApiKey.PermissionsJson,
				}
			)
			.FirstOrDefaultAsync(token);

		if (apiKey is not null && apiKey.PermissionsJson is null)
			return [];

		var memberUserId = apiKey?.OwnerUserId ?? query.UserId;

		var roles = await context.OrganizationRoleUsers
			.Where(u => u.UserId == memberUserId && u.User.IsActive && u.User.ApiKey == null)
			.Select(u => u.OrganizationRole)
			.Select(
				r => new
				{
					r.OrganizationId,
					r.Organization.TenantId,
					r.Organization.Name,
					r.Organization.IsActive,
					r.PermissionsJson,
				}
			)
			.ToListAsync(token);

		var keyPermissions = apiKey is null ? null : OrganizationManagement.ReadPermissions(apiKey.PermissionsJson);
		var organizations = new List<Organization>();

		foreach (var group in roles.GroupBy(r => (r.OrganizationId, r.TenantId, r.Name, r.IsActive)))
		{
			var membershipPermissions = group
				.SelectMany(r => OrganizationManagement.ReadPermissions(r.PermissionsJson))
				.Distinct()
				.ToArray();

			if (membershipPermissions.Length == 0)
				continue;

			var permissions = keyPermissions is null
				? membershipPermissions
				: keyPermissions
					.Where(
						p => p is OrganizationPermission.None
							|| membershipPermissions.Contains(OrganizationPermission.Admin)
							|| membershipPermissions.Contains(p)
					)
					.Distinct()
					.ToArray();

			if (!group.Key.IsActive)
				permissions = permissions.Contains(OrganizationPermission.Admin) ? [OrganizationPermission.Admin] : [];

			if (permissions.Length == 0)
				continue;

			organizations.Add(
				new()
				{
					OrganizationId = group.Key.OrganizationId,
					TenantId = group.Key.TenantId,
					Name = group.Key.Name,
					IsActive = group.Key.IsActive,
					Permissions = permissions,
				}
			);
		}

		return organizations
			.OrderBy(o => o.Name, StringComparer.OrdinalIgnoreCase)
			.ThenBy(o => o.OrganizationId.Value)
			.ToList();
	}

	public sealed record Query
	{
		public required UserId UserId { get; init; }
	}

	public sealed record Organization
	{
		public required OrganizationId OrganizationId { get; init; }

		public required TenantId TenantId { get; init; }

		public required string Name { get; init; }

		public required bool IsActive { get; init; }

		public required IReadOnlyList<OrganizationPermission> Permissions { get; init; }
	}
}
