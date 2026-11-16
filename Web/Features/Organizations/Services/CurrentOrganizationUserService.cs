using Immediate.Injections.Shared;
using VsaTemplate.Web.Features.AccessControl.Services;
using VsaTemplate.Web.Features.Organizations.Models;
using VsaTemplate.Web.Features.Shared.Extensions;

namespace VsaTemplate.Web.Features.Organizations.Services;

[RegisterScoped]
public sealed class CurrentOrganizationUserService(
	CurrentUserService currentUserService,
	OrganizationUserPermissionCache organizationUserPermissionCache
)
{
	public async ValueTask<IReadOnlyList<OrganizationPermission>> GetCurrentUserPermissions(OrganizationId organizationId) =>
		await currentUserService.GetCurrentUserId() switch
		{
			{ } userId => await organizationUserPermissionCache
				.GetValue(
					new()
					{
						UserId = userId,
						OrganizationId = organizationId,
					}
				)
				.Transform(r => r.Permissions),

			_ => [],
		};

	public async ValueTask<IReadOnlyList<OrganizationPermission>> GetLoggedInUserPermissions(OrganizationId organizationId) =>
		await organizationUserPermissionCache
			.GetValue(
				new()
				{
					UserId = await currentUserService.GetLoggedInUserId(),
					OrganizationId = organizationId,
				}
			)
			.Transform(r => r.Permissions);
}
