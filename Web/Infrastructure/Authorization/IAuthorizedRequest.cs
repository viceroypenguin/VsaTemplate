namespace VsaTemplate.Web.Infrastructure.Authorization;

public interface IAuthorizedRequest
{
	public static abstract string? Policy { get; }
}
