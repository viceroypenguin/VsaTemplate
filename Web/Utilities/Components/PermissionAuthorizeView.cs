using Microsoft.AspNetCore.Components;
using VsaTemplate.Web.Features.AccessControl.Models;
using VsaTemplate.Web.Features.AccessControl.Services;

namespace VsaTemplate.Web.Utilities.Components;

public sealed class PermissionAuthorizeView : AuthorizeViewCore
{
	[Parameter] public Permission Permission { get; set; }

	[Inject] public CurrentUserService CurrentUserService { get; set; } = default!;

	protected override async ValueTask<bool> IsAuthorizedAsync() =>
		await CurrentUserService.GetCurrentUserPermissions().HasPermission(Permission);
}
