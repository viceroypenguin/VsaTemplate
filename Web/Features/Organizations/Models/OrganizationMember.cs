using VsaTemplate.Web.Features.AccessControl.Models;

namespace VsaTemplate.Web.Features.Organizations.Models;

public sealed record OrganizationMember
{
	public required UserId UserId { get; init; }

	public required string? Name { get; init; }

	public required string EmailAddress { get; init; }

	public required IReadOnlyList<OrganizationRoleAssignment> Roles { get; init; }
}

public sealed record OrganizationRoleAssignment
{
	public required OrganizationRoleId RoleId { get; init; }

	public required string Name { get; init; }
}

public sealed record OrganizationRoleDetails
{
	public required OrganizationRoleId RoleId { get; init; }

	public required string Name { get; init; }

	public required IReadOnlyList<OrganizationPermission> Permissions { get; init; }
}
