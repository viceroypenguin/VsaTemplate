using Immediate.Apis.Shared;
using Immediate.Handlers.Shared;
using LinqToDB.Async;
using Microsoft.AspNetCore.Mvc;
using VsaTemplate.Web.Database;
using VsaTemplate.Web.Features.AccessControl.Authorization;
using VsaTemplate.Web.Features.Organizations.Queries;
using VsaTemplate.Web.Features.Organizations.Services;
using VsaTemplate.Web.Features.Tenants.Authorization;
using VsaTemplate.Web.Features.Tenants.Models;

namespace VsaTemplate.Web.Features.Organizations.Endpoints;

[Handler]
[MapGet("/api/tenants/{tenantId}/organizations")]
public sealed partial class GetOrganizationsByTenant(
	DbContext context,
	CurrentOrganizationUserService currentOrganizationUserService
)
{
	public sealed record Query : IAuthorizedRequest, ITenantRequest
	{
		public static TenantPermission TenantPermission => TenantPermission.OrganizationManagement;

		[FromRoute]
		public required TenantId TenantId { get; init; }
	}

	public sealed record Response
	{
		public required IReadOnlyList<GetOrganizationsForUser.Organization> Organizations { get; init; }
	}

	private async ValueTask<Response> HandleAsync([AsParameters] Query query, CancellationToken token)
	{
		var organizations = await context.Organizations
			.Where(o => o.TenantId == query.TenantId)
			.OrderBy(o => o.Name)
			.ThenBy(o => o.OrganizationId)
			.ToListAsync(token);
		var result = new List<GetOrganizationsForUser.Organization>(organizations.Count);
		foreach (var organization in organizations)
			result.Add(new()
			{
				OrganizationId = organization.OrganizationId,
				TenantId = organization.TenantId,
				Name = organization.Name,
				IsActive = organization.IsActive,
				Permissions = await currentOrganizationUserService.GetLoggedInUserPermissions(organization.OrganizationId),
			});
		return new() { Organizations = result };
	}
}
