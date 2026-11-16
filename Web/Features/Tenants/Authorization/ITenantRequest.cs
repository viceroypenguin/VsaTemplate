using VsaTemplate.Web.Features.Tenants.Models;

namespace VsaTemplate.Web.Features.Tenants.Authorization;

public interface ITenantRequest
{
	static virtual TenantPermission TenantPermission => TenantPermission.None;

	TenantId TenantId { get; }
}
