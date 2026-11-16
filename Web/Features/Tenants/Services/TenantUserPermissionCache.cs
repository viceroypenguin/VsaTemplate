using System.Globalization;
using Immediate.Cache.Shared;
using VsaTemplate.Web.Features.Tenants.Queries;

namespace VsaTemplate.Web.Features.Tenants.Services;

[CacheFor<TenantGetUserPermissions>]
public sealed partial class TenantUserPermissionCache
{
	protected override string TransformKey(TenantGetUserPermissions.Query request) =>
		string.Create(CultureInfo.InvariantCulture, $"TenantUserPermissionCache-{request.UserId}-{request.TenantId}");
}
