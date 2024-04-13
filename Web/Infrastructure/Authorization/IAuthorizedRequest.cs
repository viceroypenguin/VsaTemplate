namespace VsaTemplate.Web.Infrastructure.Authorization;

internal interface IAuthorizedRequest
{
	public static abstract string? Policy { get; }
}
