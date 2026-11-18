using System.Globalization;
using Immediate.Cache.Shared;
using Immediate.Handlers.Shared;
using VsaTemplate.Web.Features.AccessControl.Models;
using VsaTemplate.Web.Features.Organizations.Queries;

namespace VsaTemplate.Web.Features.Organizations.Services;

[CacheFor<GetOrganizationsByUser>]
public sealed partial class GetOrganizationsByUserCache
{
	protected override string TransformKey(GetOrganizationsByUser.Query request) =>
		string.Create(CultureInfo.InvariantCulture, $"GetOrganizationsByUserCache-{request.UserId}");
}

[Handler]
public sealed partial class GetOrganizationsByUser(
	GetOrganizationsForUser.Handler getOrganizationsForUser
)
{
	private async ValueTask<Response> HandleAsync(
		Query query,
		CancellationToken token
	)
	{
		return new()
		{
			Organizations = await getOrganizationsForUser.HandleAsync(
				new() { UserId = query.UserId },
				token
			),
		};
	}

	public sealed record Query
	{
		public required UserId UserId { get; init; }
	}

	public sealed record Response
	{
		public required IReadOnlyList<GetOrganizationsForUser.Organization> Organizations { get; init; }
	}
}
