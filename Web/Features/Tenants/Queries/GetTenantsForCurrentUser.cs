using Immediate.Handlers.Shared;
using LinqToDB;
using LinqToDB.Async;
using LinqToDB.DataProvider.SqlServer;
using VsaTemplate.Web.Database;
using VsaTemplate.Web.Features.AccessControl.Services;
using VsaTemplate.Web.Features.Shared.Extensions;
using VsaTemplate.Web.Features.Tenants.Models;

namespace VsaTemplate.Web.Features.Tenants.Queries;

[Handler]
public sealed partial class GetTenantsForCurrentUser(
	CurrentUserService currentUserService,
	DbContext context
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
		public required IReadOnlyList<TenantPermission> Permissions { get; init; }
	}

	private async ValueTask<Response> HandleAsync(
		Query _,
		CancellationToken token
	)
	{
		if (await currentUserService.GetCurrentUserId() is not { } userId)
			return new() { Tenants = [] };

		var tenants = await context.AccessControl.Users
			.Where(u => u.UserId == userId)
			.SelectMany(u => u.TenantRoleUsers)
			.Select(tru => tru.TenantRole)
			.Where(tr => tr.Tenant.IsActive)
			.SelectMany(
				tr => SqlFn.OpenJson(tr.PermissionsJson)
					.Select(jd => new { tr.TenantId, tr.Tenant.Name, jd.Value })
			)
			.Where(jd => Sql.Convert<int, string?>(jd.Value) != 0)
			.Distinct()
			.ToListAsync(token)
			.Transform(
				l => l
					.GroupBy(
						p => (p.TenantId, p.Name),
						(k, g) => new Tenant
						{
							TenantId = k.TenantId,
							Name = k.Name,
							Permissions = g
								.Select(
									v => int.TryParse(v.Value, provider: null, out var p)
										? (TenantPermission)p
										: TenantPermission.None
								)
								.ToList(),
						}
					)
					.ToList()
			);

		return new()
		{
			Tenants = tenants,
		};
	}
}
