using Microsoft.AspNetCore.Components;
using VsaTemplate.Web.Features.AccessControl.Models;
using VsaTemplate.Web.Features.AccessControl.Services;
using VsaTemplate.Web.Utilities.Components;

namespace VsaTemplate.Web.Features.AccessControl.Components;

public sealed class PermissionAuthorizeView : AuthorizeViewCore
{
	[Parameter] public Permission Permission { get; set; }

	[Inject] public CurrentUserService CurrentUserService { get; set; } = default!;

	protected override async ValueTask<bool> IsAuthorizedAsync() =>
		await CurrentUserService.GetCurrentUserPermissions().HasPermission(Permission);
}
