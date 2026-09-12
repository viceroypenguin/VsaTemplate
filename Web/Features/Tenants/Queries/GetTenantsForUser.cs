using System.Diagnostics.CodeAnalysis;
using Immediate.Handlers.Shared;
using LinqToDB;
using LinqToDB.Async;
using LinqToDB.DataProvider.SqlServer;
using VsaTemplate.Web.Database;
using VsaTemplate.Web.Features.AccessControl.Models;
using VsaTemplate.Web.Features.Shared.Extensions;
using VsaTemplate.Web.Features.Tenants.Models;

namespace VsaTemplate.Web.Features.Tenants.Queries;

[Handler]
public sealed partial class GetTenantsForUser(
	DbContext context
)
{
	public sealed record Query
	{
		public required UserId UserId { get; init; }
	}

	[SuppressMessage("Naming", "CA1724")]
	public sealed record Tenant
	{
		public required TenantId TenantId { get; init; }
		public required string Name { get; init; }
		public required IReadOnlyList<TenantPermission> Permissions { get; init; }
	}

	private async ValueTask<IReadOnlyList<Tenant>> HandleAsync(
		Query query,
		CancellationToken token
	)
	{
		return await context.AccessControl.Users
			.Where(u => u.UserId == query.UserId)
			.SelectMany(u => u.TenantRoleUsers)
			.Select(tru => tru.TenantRole)
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
	}
}
