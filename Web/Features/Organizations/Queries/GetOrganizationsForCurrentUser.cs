using Immediate.Handlers.Shared;
using VsaTemplate.Web.Features.AccessControl.Services;
using VsaTemplate.Web.Features.Organizations.Services;
using VsaTemplate.Web.Features.Tenants.Models;
using VsaTemplate.Web.Features.Tenants.Services;

namespace VsaTemplate.Web.Features.Organizations.Queries;

[Handler]
public sealed partial class GetOrganizationsForCurrentUser(
	GetOrganizationsForUser.Handler getOrganizationsForUser,
	CurrentUserService currentUserService,
	CurrentOrganizationUserService currentOrganizationUserService,
	CurrentTenantUserService currentTenantUserService
)
{
	public sealed record Query;

	private async ValueTask<IReadOnlyList<GetOrganizationsForUser.Organization>> HandleAsync(Query _, CancellationToken token)
	{
		if (await currentUserService.GetCurrentUserId() is null)
			return [];
		var memberships = await getOrganizationsForUser.HandleAsync(
			new() { UserId = await currentUserService.GetMembershipUserId() }, token);
		var organizations = new List<GetOrganizationsForUser.Organization>();
		foreach (var membership in memberships)
		{
			if (!await currentTenantUserService.GetLoggedInUserPermissions(membership.TenantId).HasPermission(TenantPermission.None))
				continue;
			var permissions = await currentOrganizationUserService.GetLoggedInUserPermissions(membership.OrganizationId);
			if (permissions.Count > 0)
				organizations.Add(membership with { Permissions = permissions });
		}

		return organizations;
	}
}
