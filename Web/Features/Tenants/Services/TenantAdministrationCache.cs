using System.Globalization;
using Immediate.Cache.Shared;
using VsaTemplate.Web.Features.Tenants.Queries;

namespace VsaTemplate.Web.Features.Tenants.Services;

[CacheFor<GetTenantAdministrationPermissions>]
public sealed partial class TenantAdministrationCache
{
	protected override string TransformKey(GetTenantAdministrationPermissions.Query request) =>
		string.Create(CultureInfo.InvariantCulture, $"TenantAdministrationCache-{request.UserId}-{request.TenantId}");
}
