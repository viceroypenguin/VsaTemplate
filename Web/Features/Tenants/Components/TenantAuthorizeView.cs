using Microsoft.AspNetCore.Components;
using VsaTemplate.Web.Features.AccessControl.Services;
using VsaTemplate.Web.Features.Tenants.Models;
using VsaTemplate.Web.Features.Tenants.Services;
using VsaTemplate.Web.Utilities.Components;

namespace VsaTemplate.Web.Features.Tenants.Components;

public sealed class TenantAuthorizeView : AuthorizeViewCore
{
	[Parameter] public TenantPermission TenantPermission { get; set; }

	[CascadingParameter] public TenantId? TenantId { get; set; }

	[Inject] public CurrentTenantUserService CurrentTenantUserService { get; set; } = default!;

	protected override async ValueTask<bool> IsAuthorizedAsync() =>
		TenantId is { } tenantId
		&& await CurrentTenantUserService.GetCurrentUserPermissions(tenantId) is { } permissions
		&& permissions.HasPermission(TenantPermission);
}
