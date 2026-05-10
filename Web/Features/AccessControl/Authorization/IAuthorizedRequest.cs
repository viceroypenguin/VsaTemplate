using VsaTemplate.Web.Features.AccessControl.Models;

namespace VsaTemplate.Web.Features.AccessControl.Authorization;

public interface IAuthorizedRequest
{
	static virtual Permission Permission { get; }
}
