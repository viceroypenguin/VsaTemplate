namespace VsaTemplate.Web.Features.AccessControl.Models;

public sealed record EmailLoginUser
{
	public required UserId UserId { get; init; }

	public required string? Name { get; init; }

	public required string EmailAddress { get; init; }
}
