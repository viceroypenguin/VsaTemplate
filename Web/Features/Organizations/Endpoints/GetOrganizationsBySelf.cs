using Immediate.Apis.Shared;
using Immediate.Handlers.Shared;
using VsaTemplate.Web.Features.AccessControl.Authorization;
using VsaTemplate.Web.Features.AccessControl.Services;
using VsaTemplate.Web.Features.Organizations.Queries;
using VsaTemplate.Web.Features.Organizations.Services;

namespace VsaTemplate.Web.Features.Organizations.Endpoints;

[Handler]
[MapGet("/api/organizations")]
public sealed partial class GetOrganizationsBySelf(
	CurrentUserService currentUserService,
	GetOrganizationsByUserCache organizationsCache
)
{
	private async ValueTask<Response> HandleAsync(Query _, CancellationToken token)
	{
		var result = await organizationsCache.GetValue(
			new() { UserId = currentUserService.GetLoggedInUserId() },
			token
		);

		return new() { Organizations = result.Organizations };
	}

	public sealed record Query : IAuthorizedRequest;

	public sealed record Response
	{
		public required IReadOnlyList<GetOrganizationsForUser.Organization> Organizations { get; init; }
	}
}
