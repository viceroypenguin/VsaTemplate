namespace VsaTemplate.Api.Features.Users.Models;

public sealed record User
{
	public UserId UserId { get; set; }
	public Auth0UserId? Auth0UserId { get; set; }
	public required string EmailAddress { get; set; }

	public string? Name { get; set; }
	public bool IsActive { get; set; }
	public DateTimeOffset? LastLogin { get; set; }

	public IReadOnlyList<string> Roles { get; set; } = [];

	public override int GetHashCode() =>
		UserId.GetHashCode();

	public bool Equals(User? other) =>
		other != null
		&& UserId.Equals(other.UserId);
}
