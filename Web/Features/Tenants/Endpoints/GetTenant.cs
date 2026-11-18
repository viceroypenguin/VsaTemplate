using Immediate.Apis.Shared;
using Immediate.Handlers.Shared;
using LinqToDB.Async;
using Microsoft.AspNetCore.Mvc;
using VsaTemplate.Web.Database;
using VsaTemplate.Web.Features.AccessControl.Authorization;
using VsaTemplate.Web.Features.Shared.Exceptions;
using VsaTemplate.Web.Features.Tenants.Authorization;
using VsaTemplate.Web.Features.Tenants.Models;

namespace VsaTemplate.Web.Features.Tenants.Endpoints;

[Handler]
[MapGet("/api/tenants/{tenantId}")]
public sealed partial class GetTenant(DbContext context)
{
	private async ValueTask<Response> HandleAsync([AsParameters] Query query, CancellationToken token) =>
		await context.Tenants
			.Where(t => t.TenantId == query.TenantId)
			.Select(
				t => new Response
				{
					TenantId = t.TenantId,
					Name = t.Name,
					IsActive = t.IsActive,
				}
			)
			.FirstOrDefaultAsync(token)
		?? throw new NotFoundException(nameof(Database.Models.Tenant));

	public sealed record Query : IAuthorizedRequest, ITenantRequest
	{
		[FromRoute]
		public required TenantId TenantId { get; init; }
	}

	public sealed record Response
	{
		public required TenantId TenantId { get; init; }

		public required string Name { get; init; }

		public required bool IsActive { get; init; }
	}
}
