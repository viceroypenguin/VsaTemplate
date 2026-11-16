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
	public ValueTask<IReadOnlyList<OrganizationPermission>> GetCurrentUserPermissions(OrganizationId organizationId) =>
		currentUserService.GetCurrentUserId() switch
		{
			{ } userId => organizationUserPermissionCache
				.GetValue(
					new()
					{
						UserId = userId,
						OrganizationId = organizationId,
					}
				)
				.Transform(r => r.Permissions),

			_ => ValueTask.FromResult<IReadOnlyList<OrganizationPermission>>([]),
		};

	public ValueTask<IReadOnlyList<OrganizationPermission>> GetLoggedInUserPermissions(OrganizationId organizationId) =>
		organizationUserPermissionCache
			.GetValue(
				new()
				{
					UserId = currentUserService.GetLoggedInUserId(),
					OrganizationId = organizationId,
				}
			)
			.Transform(r => r.Permissions);
}
