using Immediate.Handlers.Shared;
using VsaTemplate.Web.Features.AccessControl.Models;

namespace VsaTemplate.Web.Features.Organizations.Queries;

[Handler]
public sealed partial class GetOrganizationsByUser(
	GetOrganizationsForUser.Handler getOrganizationsForUser
)
{
	public sealed record Query
	{
		public required UserId UserId { get; init; }
	}

	public sealed record Response
	{
		public required IReadOnlyList<GetOrganizationsForUser.Organization> Organizations { get; init; }
	}

	private async ValueTask<Response> HandleAsync(
		Query query,
		CancellationToken token
	)
	{
		return new()
		{
			Organizations = await getOrganizationsForUser.HandleAsync(new() { UserId = query.UserId }, token),
		};
	}
}
