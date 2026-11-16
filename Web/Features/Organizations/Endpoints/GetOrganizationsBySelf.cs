using Immediate.Apis.Shared;
using Immediate.Handlers.Shared;
using Microsoft.AspNetCore.Mvc;
using VsaTemplate.Web.Features.AccessControl.Authorization;
using VsaTemplate.Web.Features.AccessControl.Services;
using VsaTemplate.Web.Features.Organizations.Services;

namespace VsaTemplate.Web.Features.VsaTemplate.Endpoints;

[Handler]
[MapGet("/api/organizations")]
public sealed partial class GetOrganizationsBySelf(
	CurrentUserService currentUserService,
	GetOrganizationsByUserCache organizationsCache
)
{
	public sealed record Query : IAuthorizedRequest;

	public sealed record Response
	{
		public required IReadOnlyList<Organization> Organizations { get; init; }
	}

	private async ValueTask<Response> HandleAsync(
		Query _,
		CancellationToken token
	) =>
		new()
		{
			Organizations = await organizationsCache.GetValue(
				new() { UserId = await currentUserService.GetLoggedInUserId() },
				token
			),
		};
}
