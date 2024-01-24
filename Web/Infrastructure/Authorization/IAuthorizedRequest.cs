using Microsoft.AspNetCore.Authorization;

namespace VsaTemplate.Web.Infrastructure.Authorization;

public interface IAuthorizedRequest
{
	public static abstract string? Policy { get; }
	public static abstract IAuthorizationRequirement? Requirement { get; }
}
