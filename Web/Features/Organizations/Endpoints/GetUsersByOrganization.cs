using Immediate.Apis.Shared;
using Immediate.Handlers.Shared;
using LinqToDB.Async;
using Microsoft.AspNetCore.Mvc;
using VsaTemplate.Web.Database;
using VsaTemplate.Web.Features.AccessControl.Authorization;
using VsaTemplate.Web.Features.Organizations.Authorization;
using VsaTemplate.Web.Features.Organizations.Models;

namespace VsaTemplate.Web.Features.Organizations.Endpoints;

[Handler]
[MapGet("/api/organizations/{organizationId}/users")]
public sealed partial class GetUsersByOrganization(DbContext context)
{
	private async ValueTask<Response> HandleAsync([AsParameters] Query query, CancellationToken token)
	{
		var memberships = await context.OrganizationRoleUsers
			.Where(u => u.OrganizationRole.OrganizationId == query.OrganizationId && u.User.ApiKey == null)
			.Select(u => new
				{
					u.UserId,
					u.User.Name,
					u.User.EmailAddress,
					RoleId = u.OrganizationRoleId,
					RoleName = u.OrganizationRole.Name,
				}
			)
			.ToListAsync(token);

		return new()
		{
			Users = memberships
				.GroupBy(u => (u.UserId, u.Name, u.EmailAddress))
				.Select(g => new OrganizationMember
					{
						UserId = g.Key.UserId,
						Name = g.Key.Name,
						EmailAddress = g.Key.EmailAddress,
						Roles = g
							.Select(u => new OrganizationRoleAssignment
								{
									RoleId = u.RoleId,
									Name = u.RoleName,
								}
							)
							.OrderBy(r => r.Name, StringComparer.OrdinalIgnoreCase)
							.ToList(),
					}
				)
				.OrderBy(u => u.EmailAddress, StringComparer.OrdinalIgnoreCase)
				.ToList(),
		};
	}

	public sealed record Query : IAuthorizedRequest, IOrganizationRequest
	{
		public static OrganizationPermission OrganizationPermission => OrganizationPermission.UserManagement;

		[FromRoute]
		public required OrganizationId OrganizationId { get; init; }
	}

	public sealed record Response
	{
		public required IReadOnlyList<OrganizationMember> Users { get; init; }
	}
}
