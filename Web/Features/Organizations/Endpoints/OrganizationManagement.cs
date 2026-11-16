using System.Text.Json;
using LinqToDB.Async;
using VsaTemplate.Web.Database;
using VsaTemplate.Web.Features.AccessControl.Models;
using VsaTemplate.Web.Features.Organizations.Models;
using VsaTemplate.Web.Features.Organizations.Services;
using VsaTemplate.Web.Features.Shared.Exceptions;
using VsaTemplate.Web.Infrastructure.Exceptions;

namespace VsaTemplate.Web.Features.Organizations.Endpoints;

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
		return permissions.Distinct().ToArray();
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
			.Where(u => adminRoleIds.Contains(u.OrganizationRoleId) && u.User.IsActive && u.User.ApiKey == null)
			.Where(u => u.User.TenantRoleUsers
				.Any(t => t.TenantRole.TenantId == u.OrganizationRole.Organization.TenantId));
		if (removingUserId is { } userId)
			administrators = administrators.Where(u => u.UserId != userId);
		if (!await administrators.AnyAsync(token))
			ConflictException.ThrowConflictException("An organization must retain at least one active administrator.");
	}

	public static async Task EnsureTenantMember(
		DbContext context,
		OrganizationId organizationId,
		UserId userId,
		CancellationToken token
	)
	{
		var organization = await context.Organizations.FirstOrDefaultAsync(o => o.OrganizationId == organizationId, token)
			?? throw new NotFoundException(nameof(Database.Models.Organization));

		if (!await context.TenantRoleUsers
			.AnyAsync(u => u.UserId == userId && u.User.IsActive && u.User.ApiKey == null
				&& u.TenantRole.TenantId == organization.TenantId, token))
			NotFoundException.ThrowNotFoundException("Tenant member", "The user must first be added to the parent tenant.");
	}
}

file sealed class InvalidOrganizationInputException(string message) : VsaTemplateException(message, 400);
