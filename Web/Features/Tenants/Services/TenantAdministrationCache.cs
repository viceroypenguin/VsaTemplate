using System.Globalization;
using Immediate.Cache.Shared;
using Immediate.Handlers.Shared;
using VsaTemplate.Web.Features.AccessControl.Models;
using VsaTemplate.Web.Features.Tenants.Models;

namespace VsaTemplate.Web.Features.Tenants.Services;

[CacheFor<GetTenantAdministrationPermissions>]
public sealed partial class TenantAdministrationCache
{
	protected override string TransformKey(GetTenantAdministrationPermissions.Query request) =>
		string.Create(CultureInfo.InvariantCulture, $"TenantAdministrationCache-{request.UserId}-{request.TenantId}");
}

[Handler]
public sealed partial class GetTenantAdministrationPermissions(TenantPermissionReader permissions)
{
	private async ValueTask<Response> HandleAsync(Query query, CancellationToken token)
	{
		var effectivePermissions = await permissions.Read(query.UserId, query.TenantId, includeInactive: true, token);

		return new()
		{
			Permissions = effectivePermissions.Contains(TenantPermission.Admin) ? [TenantPermission.Admin] : [],
		};
	}

	public sealed record Query
	{
		public required UserId UserId { get; init; }

		public required TenantId TenantId { get; init; }
	}

	public sealed record Response
	{
		public required IReadOnlyList<TenantPermission> Permissions { get; init; }
	}
}
