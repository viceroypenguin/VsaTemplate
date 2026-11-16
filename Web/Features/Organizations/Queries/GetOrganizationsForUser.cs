using Immediate.Handlers.Shared;
using LinqToDB.Async;
using VsaTemplate.Web.Database;
using VsaTemplate.Web.Features.AccessControl.Models;
using VsaTemplate.Web.Features.Organizations.Endpoints;
using VsaTemplate.Web.Features.Organizations.Models;
using VsaTemplate.Web.Features.Tenants.Models;

namespace VsaTemplate.Web.Features.Organizations.Queries;

[Handler]
public sealed partial class GetOrganizationsForUser(DbContext context)
{
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

	private async ValueTask<IReadOnlyList<Organization>> HandleAsync(Query query, CancellationToken token)
	{
		var roles = await context.OrganizationRoleUsers
			.Where(u => u.UserId == query.UserId && u.User.IsActive)
			.Select(u => u.OrganizationRole)
			.Where(r => r.Organization.Tenant.IsActive
				&& r.Organization.Tenant.TenantRoles.SelectMany(t => t.TenantRoleUsers)
				.Any(u => u.UserId == query.UserId))
			.Select(r => new
			{
				r.OrganizationId,
				r.Organization.TenantId,
				r.Organization.Name,
				r.Organization.IsActive,
				r.PermissionsJson,
			})
			.ToListAsync(token);

		return roles
			.GroupBy(r => (r.OrganizationId, r.TenantId, r.Name, r.IsActive))
			.Select(g => new Organization
			{
				OrganizationId = g.Key.OrganizationId,
				TenantId = g.Key.TenantId,
				Name = g.Key.Name,
				IsActive = g.Key.IsActive,
				Permissions = g.SelectMany(r => OrganizationManagement.ReadPermissions(r.PermissionsJson)).Distinct().ToArray(),
			})
			.Where(o => o.Permissions.Count > 0)
			.OrderBy(o => o.Name, StringComparer.OrdinalIgnoreCase)
			.ThenBy(o => o.OrganizationId.Value)
			.ToList();
	}
}
