using System.Text.Json;
using LinqToDB.Async;
using VsaTemplate.Web.Database;
using VsaTemplate.Web.Features.AccessControl.Models;
using VsaTemplate.Web.Features.Organizations.Exceptions;
using VsaTemplate.Web.Features.Organizations.Models;
using VsaTemplate.Web.Features.Shared.Exceptions;

namespace VsaTemplate.Web.Features.Organizations.Services;

internal static class OrganizationManagement
{
	public static string ValidateName(string? name)
	{
		if (string.IsNullOrWhiteSpace(name) || name.Trim().Length > 200)
			throw new InvalidOrganizationInputException("Name is required and must contain at most 200 characters.");

		return name.Trim();
	}

	public static OrganizationPermission[] ValidatePermissions(IReadOnlyList<OrganizationPermission>? permissions)
	{
		if (permissions is null || permissions.Count == 0 || permissions.Any(p => !Enum.IsDefined(p)))
			throw new InvalidOrganizationInputException("Select at least one valid organization permission.");

		return permissions
			.Distinct()
			.ToArray();
	}

	public static OrganizationPermission[] ReadPermissions(string json) =>
		JsonSerializer.Deserialize<OrganizationPermission[]>(json) ?? [];

	public static async Task DemandDelegablePermissions(
		CurrentOrganizationUserService currentOrganizationUserService,
		OrganizationId organizationId,
		IReadOnlyList<OrganizationPermission> permissions
	)
	{
		var current = await currentOrganizationUserService.GetLoggedInUserPermissions(organizationId);

		if (!current.Contains(OrganizationPermission.Admin)
			&& permissions.Any(p => p != OrganizationPermission.None && !current.Contains(p)))
			UnauthorizedException.ThrowUnauthorizedException("You cannot grant or change permissions beyond your own.");
	}

	public static async Task<Database.Models.OrganizationRole> GetRole(
		DbContext context,
		OrganizationId organizationId,
		OrganizationRoleId roleId,
		CancellationToken token
	) => await context.OrganizationRoles
		.FirstOrDefaultAsync(r => r.OrganizationId == organizationId && r.OrganizationRoleId == roleId, token)
		?? throw new NotFoundException(nameof(Database.Models.OrganizationRole));

	public static async Task EnsureAdministratorRemains(
		DbContext context,
		OrganizationId organizationId,
		UserId? removingUserId,
		OrganizationRoleId? changingRoleId,
		CancellationToken token
	)
	{
		var roles = await context.OrganizationRoles
			.Where(r => r.OrganizationId == organizationId)
			.ToListAsync(token);

		var adminRoleIds = roles
			.Where(r => (changingRoleId is null || r.OrganizationRoleId != changingRoleId.Value)
				&& ReadPermissions(r.PermissionsJson).Contains(OrganizationPermission.Admin))
			.Select(r => r.OrganizationRoleId)
			.ToArray();

		var administrators = context.OrganizationRoleUsers
			.Where(u => adminRoleIds.Contains(u.OrganizationRoleId) && u.User.IsActive && u.User.ApiKey == null);

		if (removingUserId is { } userId)
			administrators = administrators
				.Where(u => u.UserId != userId);

		if (!await administrators.AnyAsync(token))
			ConflictException.ThrowConflictException("An organization must retain at least one active administrator.");
	}

	public static async Task EnsureActiveUser(
		DbContext context,
		UserId userId,
		CancellationToken token
	)
	{
		if (!await context.AccessControl.Users.AnyAsync(u => u.UserId == userId && u.IsActive && u.ApiKey == null, token))
			NotFoundException.ThrowNotFoundException("User");
	}
}
