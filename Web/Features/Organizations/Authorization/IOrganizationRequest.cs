using VsaTemplate.Web.Features.Organizations.Models;

namespace VsaTemplate.Web.Features.Organizations.Authorization;

public interface IOrganizationRequest
{
	static virtual OrganizationPermission OrganizationPermission => OrganizationPermission.None;

	OrganizationId OrganizationId { get; }
}
