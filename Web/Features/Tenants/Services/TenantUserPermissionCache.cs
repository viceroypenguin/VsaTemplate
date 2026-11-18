using System.Globalization;
using Immediate.Cache.Shared;
using Immediate.Handlers.Shared;
using VsaTemplate.Web.Features.AccessControl.Models;
using VsaTemplate.Web.Features.Tenants.Models;

namespace VsaTemplate.Web.Features.Tenants.Services;

[CacheFor<TenantGetUserPermissions>]
public sealed partial class TenantUserPermissionCache
{
	protected override string TransformKey(TenantGetUserPermissions.Query request) =>
		string.Create(CultureInfo.InvariantCulture, $"TenantUserPermissionCache-{request.UserId}-{request.TenantId}");
}

[Handler]
public sealed partial class TenantGetUserPermissions(TenantPermissionReader permissions)
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

	private async ValueTask<Response> HandleAsync(Query query, CancellationToken token) => new()
	{
		Permissions = await permissions.Read(query.UserId, query.TenantId, includeInactive: false, token),
	};
}
