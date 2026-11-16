using Immediate.Apis.Shared;
using Immediate.Handlers.Shared;
using VsaTemplate.Web.Features.AccessControl.Authorization;
using VsaTemplate.Web.Features.Shared.Extensions;
using VsaTemplate.Web.Features.Tenants.Models;
using VsaTemplate.Web.Features.Tenants.Queries;

namespace VsaTemplate.Web.Features.Tenants.Endpoints;

[Handler]
[MapGet("/api/tenants")]
public sealed partial class GetTenants(
	GetTenantsForCurrentUser.Handler getTenantsForCurrentUser
)
{
	public sealed record Query : IAuthorizedRequest;

	public sealed record Response
	{
		public required IReadOnlyList<Tenant> Tenants { get; init; }
	}

	public sealed record Tenant
	{
		public required TenantId TenantId { get; init; }
		public required string Name { get; init; }
		public required bool IsActive { get; init; }
		public required IReadOnlyList<TenantPermission> Permissions { get; init; }
	}

	private async ValueTask<Response> HandleAsync(
		Query _,
		CancellationToken token
	) => new()
	{
		Tenants = await getTenantsForCurrentUser.HandleAsync(new(), token)
			.Transform(x => x.Tenants
				.Select(t => new Tenant
				{
					TenantId = t.TenantId,
					Name = t.Name,
					IsActive = t.IsActive,
					Permissions = t.Permissions,
				})
				.ToList()
			),
	};
}
