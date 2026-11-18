using Immediate.Apis.Shared;
using Immediate.Handlers.Shared;
using LinqToDB.Async;
using Microsoft.AspNetCore.Mvc;
using VsaTemplate.Web.Database;
using VsaTemplate.Web.Features.AccessControl.Authorization;
using VsaTemplate.Web.Features.Tenants.Authorization;
using VsaTemplate.Web.Features.Tenants.Models;
using VsaTemplate.Web.Features.Tenants.Services;

namespace VsaTemplate.Web.Features.Tenants.Endpoints;

[Handler]
[MapGet("/api/tenants/{tenantId}/roles")]
public sealed partial class GetTenantRoles(DbContext context)
{
	private async ValueTask<Response> HandleAsync([AsParameters] Query query, CancellationToken token)
	{
		var roles = await context.TenantRoles
			.Where(r => r.TenantId == query.TenantId)
			.OrderBy(r => r.Name)
			.ToListAsync(token);

		return new()
		{
			Roles = roles
				.Select(r => new Role
					{
						RoleId = r.TenantRoleId,
						Name = r.Name,
						Permissions = TenantManagement.ReadPermissions(r.PermissionsJson),
					}
				)
				.ToList(),
		};
	}

	public sealed record Query : IAuthorizedRequest, ITenantRequest
	{
		public static TenantPermission TenantPermission => TenantPermission.UserManagement;

		[FromRoute]
		public required TenantId TenantId { get; init; }
	}

	public sealed record Response
	{
		public required IReadOnlyList<Role> Roles { get; init; }
	}

	public sealed record Role
	{
		public required TenantRoleId RoleId { get; init; }

		public required string Name { get; init; }

		public required IReadOnlyList<TenantPermission> Permissions { get; init; }
	}
}
