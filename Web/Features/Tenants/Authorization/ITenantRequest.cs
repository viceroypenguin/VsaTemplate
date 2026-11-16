using VsaTemplate.Web.Features.Tenants.Models;

namespace VsaTemplate.Web.Features.Tenants.Authorization;

public interface ITenantRequest
{
	static abstract TenantPermission TenantPermission { get; }

	TenantId TenantId { get; }
}
