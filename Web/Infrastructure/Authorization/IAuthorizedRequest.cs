namespace VsaTemplate.Web.Infrastructure.Authorization;

public interface IAuthorizedRequest
{
	static abstract string? Policy { get; }
}
