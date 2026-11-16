using VsaTemplate.Web.Features.Organizations.Models;

namespace VsaTemplate.Web.Features.Organizations.Authorization;

public interface IOrganizationRequest
{
	static abstract OrganizationPermission OrganizationPermission { get; }

	OrganizationId OrganizationId { get; }
}
