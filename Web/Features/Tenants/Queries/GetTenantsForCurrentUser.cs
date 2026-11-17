using Immediate.Handlers.Shared;
using LinqToDB.Async;
using VsaTemplate.Web.Database;
using VsaTemplate.Web.Features.AccessControl.Services;
using VsaTemplate.Web.Features.Tenants.Models;
using VsaTemplate.Web.Features.Tenants.Services;

namespace VsaTemplate.Web.Features.Tenants.Queries;

[Handler]
public sealed partial class GetTenantsForCurrentUser(
	CurrentUserService currentUserService,
	DbContext context,
	CurrentTenantUserService currentTenantUserService,
	TenantAdministrationCache administration
)
{
	public sealed record Query;

	public sealed record Response
	{
		public required IReadOnlyList<Tenant> Tenants { get; init; }
	}

	public sealed record Tenant
	{
		public required TenantId TenantId { get; init; }

		public required string Name { get; init; }

		public required bool IsActive { get; init; }

		public required IReadOnlyList<TenantPermission> Permissions { get; init; }
	}

	private async ValueTask<Response> HandleAsync(Query _, CancellationToken token)
	{
		if (currentUserService.GetCurrentUserId() is null)
			return new() { Tenants = [] };

		var userId = await currentUserService.GetMembershipUserId();

		var roles = await context.TenantRoleUsers
			.Where(u => u.UserId == userId && u.User.IsActive)
			.Select(u => new
			{
				u.TenantRole.TenantId,
				u.TenantRole.Tenant.Name,
				u.TenantRole.Tenant.IsActive,
			})
			.ToListAsync(token);

		var tenants = new List<Tenant>();

		foreach (var group in roles.GroupBy(r => (r.TenantId, r.Name, r.IsActive)))
		{
			var permissions = group.Key.IsActive
				? await currentTenantUserService.GetLoggedInUserPermissions(group.Key.TenantId)
				: (await administration.GetValue(
					new() { UserId = currentUserService.GetLoggedInUserId(), TenantId = group.Key.TenantId },
					token
				)).Permissions;

			if (permissions.Count == 0)
				continue;

			tenants.Add(new()
			{
				TenantId = group.Key.TenantId,
				Name = group.Key.Name,
				IsActive = group.Key.IsActive,
				Permissions = permissions,
			});
		}

		return new()
		{
			Tenants = tenants
				.OrderBy(t => t.Name, StringComparer.OrdinalIgnoreCase)
				.ToList(),
		};
	}
}
