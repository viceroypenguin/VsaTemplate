using System.Diagnostics.CodeAnalysis;
using Immediate.Handlers.Shared;
using LinqToDB;
using LinqToDB.Async;
using LinqToDB.DataProvider.SqlServer;
using VsaTemplate.Web.Database;
using VsaTemplate.Web.Features.AccessControl.Models;
using VsaTemplate.Web.Features.Shared.Extensions;
using VsaTemplate.Web.Features.Organizations.Models;

namespace VsaTemplate.Web.Features.Organizations.Queries;

[Handler]
public sealed partial class GetOrganizationsForUser(
	DbContext context
)
{
	public sealed record Query
	{
		public required UserId UserId { get; init; }
	}

	[SuppressMessage("Naming", "CA1724")]
	public sealed record Organization
	{
		public required OrganizationId OrganizationId { get; init; }
		public required string Name { get; init; }
		public required IReadOnlyList<OrganizationPermission> Permissions { get; init; }
	}

	private async ValueTask<IReadOnlyList<Organization>> HandleAsync(
		Query query,
		CancellationToken token
	)
	{
		return await context.AccessControl.Users
			.Where(u => u.UserId == query.UserId)
			.SelectMany(u => u.OrganizationRoleUsers)
			.Select(tru => tru.OrganizationRole)
			.SelectMany(
				tr => SqlFn.OpenJson(tr.PermissionsJson)
					.Select(jd => new { tr.OrganizationId, tr.Organization.Name, jd.Value })
			)
			.Where(jd => Sql.Convert<int, string?>(jd.Value) != 0)
			.Distinct()
			.ToListAsync(token)
			.Transform(
				l => l
					.GroupBy(
						p => (p.OrganizationId, p.Name),
						(k, g) => new Organization
						{
							OrganizationId = k.OrganizationId,
							Name = k.Name,
							Permissions = g
								.Select(
									v => int.TryParse(v.Value, provider: null, out var p)
										? (OrganizationPermission)p
										: OrganizationPermission.None
								)
								.ToList(),
						}
					)
					.ToList()
			);
	}
}
