namespace VsaTemplate.Web.Infrastructure.Authorization;

public interface IAuthorizedRequest
{
	static virtual string? Policy { get; } = Policies.ValidUser;
}
