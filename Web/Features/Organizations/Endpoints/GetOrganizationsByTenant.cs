using Immediate.Apis.Shared;
using Immediate.Handlers.Shared;
using LinqToDB;
using Microsoft.AspNetCore.Mvc;
using VsaTemplate.Web.Database;
using VsaTemplate.Web.Features.AccessControl.Authorization;
using VsaTemplate.Web.Features.Tenants.Authorization;
using VsaTemplate.Web.Features.Tenants.Models;

namespace VsaTemplate.Web.Features.VsaTemplate.Endpoints;

[Handler]
[MapGet("/api/tenant/{tenantId}/organizations")]
public sealed partial class GetOrganizationsByTenant(DbContext context)
{
	public sealed record Query : IAuthorizedRequest, ITenantRequest
	{
		public TenantPermission Permission => TenantPermission.OrganizationManagement;

		[FromRoute]
		public required TenantId TenantId { get; init; }
	}

	public sealed record Response
	{
		public required IReadOnlyList<Organization> Organizations { get; init; }
	}

	private async ValueTask<Response> HandleAsync(
		Query query,
		CancellationToken token
	) =>
		new()
		{
			Organizations = await context.Organizations
				.Where(o => o.TenantId == query.TenantId)
				.Select(Organization.FromDatabaseEntity)
				.ToListAsync(token),
		};
}
