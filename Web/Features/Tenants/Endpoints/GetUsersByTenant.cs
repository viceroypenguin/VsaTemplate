using Immediate.Apis.Shared;
using Immediate.Handlers.Shared;
using LinqToDB;
using VsaTemplate.Web.Database;
using VsaTemplate.Web.Features.AccessControl.Authorization;
using VsaTemplate.Web.Features.Tenants.Authorization;
using VsaTemplate.Web.Features.Tenants.Models;

namespace VsaTemplate.Web.Features.Tenants.Endpoints;

[Handler]
[MapGet("/api/tenants/{tenantId}/users")]
public sealed partial class GetUsersByTenant(DbContext context)
{
	public sealed record Query : IAuthorizedRequest, ITenantRequest
	{
		public TenantPermission TenantPermission => TenantPermission.UserManagement;

		public required TenantId TenantId { get; init; }
	}

	public sealed record Response
	{
		public required IReadOnlyList<User> Users { get; init; }
	}

	private async ValueTask<Response> HandleAsync(
		Query query,
		CancellationToken token
	) =>
		new()
		{
			Users = await context.AccountUsers
				.Where(au => au.AccountId == query.AccountId)
				.Select(au => au.User)
				.Select(User.FromDatabaseEntity)
				.ToListAsync(token),
		};
}
