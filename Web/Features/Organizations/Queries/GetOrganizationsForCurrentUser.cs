using Immediate.Handlers.Shared;
using VsaTemplate.Web.Features.AccessControl.Services;

namespace VsaTemplate.Web.Features.Organizations.Queries;

[Handler]
public sealed partial class GetOrganizationsForCurrentUser(
	GetOrganizationsForUser.Handler getOrganizationsForUser,
	CurrentUserService currentUserService
)
{
	public sealed record Query;

	private async ValueTask<IReadOnlyList<GetOrganizationsForUser.Organization>> HandleAsync(Query _, CancellationToken token)
	{
		return currentUserService.GetCurrentUserId() is { } userId
			? await getOrganizationsForUser.HandleAsync(new() { UserId = userId }, token)
			: [];
	}
}
