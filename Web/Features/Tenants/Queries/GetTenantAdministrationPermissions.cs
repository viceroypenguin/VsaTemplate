using Immediate.Handlers.Shared;
using VsaTemplate.Web.Features.AccessControl.Models;
using VsaTemplate.Web.Features.Tenants.Models;
using VsaTemplate.Web.Features.Tenants.Services;

namespace VsaTemplate.Web.Features.Tenants.Queries;

[Handler]
public sealed partial class GetTenantAdministrationPermissions(TenantPermissionReader permissions)
{
	public sealed record Query
	{
		public required UserId UserId { get; init; }
		public required TenantId TenantId { get; init; }
	}

	public sealed record Response
	{
		public required IReadOnlyList<TenantPermission> Permissions { get; init; }
	}

	private async ValueTask<Response> HandleAsync(Query query, CancellationToken token)
	{
		var effectivePermissions = await permissions.Read(query.UserId, query.TenantId, includeInactive: true, token);

		return new()
		{
			Permissions = effectivePermissions.Contains(TenantPermission.Admin) ? [TenantPermission.Admin] : [],
		};

	}
}
