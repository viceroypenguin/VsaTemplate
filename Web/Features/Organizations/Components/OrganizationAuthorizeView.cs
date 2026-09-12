using Microsoft.AspNetCore.Components;
using VsaTemplate.Web.Features.AccessControl.Services;
using VsaTemplate.Web.Features.Organizations.Models;
using VsaTemplate.Web.Features.Organizations.Services;
using VsaTemplate.Web.Utilities.Components;

namespace VsaTemplate.Web.Features.Organizations.Components;

public sealed class OrganizationAuthorizeView : AuthorizeViewCore
{
	[Parameter] public OrganizationPermission OrganizationPermission { get; set; }

	[CascadingParameter] public OrganizationId? OrganizationId { get; set; }

	[Inject] public CurrentOrganizationUserService CurrentTenantUserService { get; set; } = default!;

	protected override async ValueTask<bool> IsAuthorizedAsync() =>
		OrganizationId is { } organizationId
		&& await CurrentTenantUserService.GetCurrentUserPermissions(organizationId) is { } permissions
		&& permissions.HasPermission(OrganizationPermission);
}
