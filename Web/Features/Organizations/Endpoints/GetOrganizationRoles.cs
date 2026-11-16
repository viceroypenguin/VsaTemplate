using Immediate.Apis.Shared;
using Immediate.Handlers.Shared;
using LinqToDB.Async;
using Microsoft.AspNetCore.Mvc;
using VsaTemplate.Web.Database;
using VsaTemplate.Web.Features.AccessControl.Authorization;
using VsaTemplate.Web.Features.Organizations.Authorization;
using VsaTemplate.Web.Features.Organizations.Models;

using VsaTemplate.Web.Features.Organizations.Services;

namespace VsaTemplate.Web.Features.Organizations.Endpoints;

[Handler]
[MapGet("/api/organizations/{organizationId}/roles")]
public sealed partial class GetOrganizationRoles(DbContext context)
{
	public sealed record Query : IAuthorizedRequest, IOrganizationRequest
	{
		public static OrganizationPermission OrganizationPermission => OrganizationPermission.UserManagement;

		[FromRoute]
		public required OrganizationId OrganizationId { get; init; }
	}

	public sealed record Response
	{
		public required IReadOnlyList<OrganizationRoleDetails> Roles { get; init; }
	}

	private async ValueTask<Response> HandleAsync([AsParameters] Query query, CancellationToken token)
	{
		var roles = await context.OrganizationRoles
			.Where(r => r.OrganizationId == query.OrganizationId)
			.OrderBy(r => r.Name)
			.ThenBy(r => r.OrganizationRoleId)
			.ToListAsync(token);

		return new()
		{
			Roles = roles
				.Select(r => new OrganizationRoleDetails
				{
					RoleId = r.OrganizationRoleId,
					Name = r.Name,
					Permissions = OrganizationManagement.ReadPermissions(r.PermissionsJson),
				})
				.ToList(),
		};
	}
}
