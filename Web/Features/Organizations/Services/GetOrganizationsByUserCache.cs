using System.Globalization;
using Immediate.Cache.Shared;
using Immediate.Handlers.Shared;
using LinqToDB;
using VsaTemplate.Web.Database;
using VsaTemplate.Web.Features.AccessControl.Models;

namespace VsaTemplate.Web.Features.Organizations.Services;

[CacheFor<GetOrganizationsByUser>]
public sealed partial class GetOrganizationsByUserCache
{
	protected override string TransformKey(GetOrganizationsByUser.Query request) =>
		string.Create(CultureInfo.InvariantCulture, $"GetOrganizationsByUserCache-{request.UserId}");
}

[Handler]
public sealed partial class GetOrganizationsByUser(
	DbContext context
)
{
	public sealed record Query
	{
		public required UserId UserId { get; init; }
	}

	public sealed record Response
	{
		public required IReadOnlyList<Organization> Organizations { get; init; }
	}

	private async ValueTask<Response> HandleAsync(
		Query query,
		CancellationToken token
	)
	{
		var userId = query.UserId;
		var organizations = context.Organizations
			.Where(o => o.OrganizationRoles
				.SelectMany(or => or.OrganizationRoleUsers)
				.Any(oru => oru.UserId == userId)
			);

		return new()
		{
			Organizations = await organizations
				.Select(Organization.FromDatabaseEntity)
				.ToListAsync(token),
		};
	}
}
