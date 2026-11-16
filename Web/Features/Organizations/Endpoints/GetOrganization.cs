using Immediate.Apis.Shared;
using Immediate.Handlers.Shared;
using LinqToDB.Async;
using Microsoft.AspNetCore.Mvc;
using VsaTemplate.Web.Database;
using VsaTemplate.Web.Features.AccessControl.Authorization;
using VsaTemplate.Web.Features.Organizations.Authorization;
using VsaTemplate.Web.Features.Organizations.Models;
using VsaTemplate.Web.Features.Shared.Exceptions;
using VsaTemplate.Web.Features.Tenants.Models;

namespace VsaTemplate.Web.Features.Organizations.Endpoints;

[Handler]
[MapGet("/api/organizations/{organizationId}")]
public sealed partial class GetOrganization(DbContext context)
{
	public sealed record Query : IAuthorizedRequest, IOrganizationRequest
	{
		[FromRoute]
		public required OrganizationId OrganizationId { get; init; }
	}

	public sealed record Response
	{
		public required OrganizationId OrganizationId { get; init; }
		public required TenantId TenantId { get; init; }
		public required string Name { get; init; }
		public required bool IsActive { get; init; }
	}

	private async ValueTask<Response> HandleAsync([AsParameters] Query query, CancellationToken token)
	{

		return await context.Organizations
			.Where(o => o.OrganizationId == query.OrganizationId)
			.Select(o => new Response
			{
				OrganizationId = o.OrganizationId,
				TenantId = o.TenantId,
				Name = o.Name,
				IsActive = o.IsActive,
			})
			.FirstOrDefaultAsync(token)
			?? throw new NotFoundException(nameof(Database.Models.Organization));

	}
}
