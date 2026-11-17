using Immediate.Apis.Shared;
using Immediate.Handlers.Shared;
using LinqToDB.Async;
using Microsoft.AspNetCore.Mvc;
using VsaTemplate.Web.Database;
using VsaTemplate.Web.Features.AccessControl.Authorization;
using VsaTemplate.Web.Features.AccessControl.Models;
using VsaTemplate.Web.Features.Tenants.Authorization;
using VsaTemplate.Web.Features.Tenants.Models;

namespace VsaTemplate.Web.Features.Tenants.Endpoints;

[Handler]
[MapGet("/api/tenants/{tenantId}/users")]
public sealed partial class GetUsersByTenant(DbContext context)
{
	public sealed record Query : IAuthorizedRequest, ITenantRequest
	{
		public static TenantPermission TenantPermission => TenantPermission.UserManagement;

		[FromRoute]
		public required TenantId TenantId { get; init; }
	}

	public sealed record Response
	{
		public required IReadOnlyList<User> Users { get; init; }
	}

	public sealed record User
	{
		public required UserId UserId { get; init; }

		public required string? Name { get; init; }

		public required string EmailAddress { get; init; }

		public required IReadOnlyList<Role> Roles { get; init; }
	}

	public sealed record Role
	{
		public required TenantRoleId RoleId { get; init; }

		public required string Name { get; init; }
	}

	private async ValueTask<Response> HandleAsync([AsParameters] Query query, CancellationToken token)
	{
		var memberships = await context.TenantRoleUsers
			.Where(u => u.TenantRole.TenantId == query.TenantId && u.User.ApiKey == null)
			.Select(u => new
			{
				u.UserId,
				u.User.Name,
				u.User.EmailAddress,
				RoleId = u.TenantRoleId,
				RoleName = u.TenantRole.Name,
			})
			.ToListAsync(token);

		return new()
		{
			Users = memberships
				.GroupBy(u => (u.UserId, u.Name, u.EmailAddress))
				.Select(g => new User
				{
					UserId = g.Key.UserId,
					Name = g.Key.Name,
					EmailAddress = g.Key.EmailAddress,
					Roles = g
						.Select(u => new Role { RoleId = u.RoleId, Name = u.RoleName })
						.OrderBy(r => r.Name, StringComparer.OrdinalIgnoreCase)
						.ToList(),
				})
				.OrderBy(u => u.EmailAddress, StringComparer.OrdinalIgnoreCase)
				.ToList(),
		};
	}
}
