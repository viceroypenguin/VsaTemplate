using Immediate.Handlers.Shared;
using VsaTemplate.Web.Features.AccessControl.Services;

namespace VsaTemplate.Web.Features.Tenants.Queries;

[Handler]
public sealed partial class GetTenantsForCurrentUser(
	GetTenantsForUser.Handler getTenantsForUser,
	CurrentUserService currentUserService
)
{
	public sealed record Query;

	private async ValueTask<IReadOnlyList<GetTenantsForUser.Tenant>> HandleAsync(
		Query _,
		CancellationToken token
	)
	{
		return await currentUserService.GetCurrentUserId() switch
		{
			{ } userId => await getTenantsForUser.HandleAsync(new() { UserId = userId }, token),
			_ => [],
		};
	}
}
