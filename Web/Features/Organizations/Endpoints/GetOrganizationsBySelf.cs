using Immediate.Apis.Shared;
using Immediate.Handlers.Shared;
using VsaTemplate.Web.Features.AccessControl.Authorization;
using VsaTemplate.Web.Features.AccessControl.Services;
using VsaTemplate.Web.Features.Organizations.Queries;
using VsaTemplate.Web.Features.Organizations.Services;
using VsaTemplate.Web.Features.Tenants.Models;
using VsaTemplate.Web.Features.Tenants.Services;

namespace VsaTemplate.Web.Features.Organizations.Endpoints;

[Handler]
[MapGet("/api/organizations")]
public sealed partial class GetOrganizationsBySelf(
	CurrentUserService currentUserService,
	CurrentOrganizationUserService currentOrganizationUserService,
	CurrentTenantUserService currentTenantUserService,
	GetOrganizationsByUserCache organizationsCache
)
{
	public sealed record Query : IAuthorizedRequest;

	public sealed record Response
	{
		public required IReadOnlyList<GetOrganizationsForUser.Organization> Organizations { get; init; }
	}

	private async ValueTask<Response> HandleAsync(Query _, CancellationToken token)
	{
		var memberships = await organizationsCache.GetValue(
			new() { UserId = await currentUserService.GetMembershipUserId() }, token);
		var organizations = new List<GetOrganizationsForUser.Organization>();
		foreach (var membership in memberships.Organizations)
		{
			if (!await currentTenantUserService.GetLoggedInUserPermissions(membership.TenantId).HasPermission(TenantPermission.None))
				continue;
			var permissions = await currentOrganizationUserService.GetLoggedInUserPermissions(membership.OrganizationId);
			if (permissions.Count > 0)
				organizations.Add(membership with { Permissions = permissions });
		}

		return new() { Organizations = organizations };
	}
}
