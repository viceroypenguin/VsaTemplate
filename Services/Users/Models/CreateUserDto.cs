namespace VsaTemplate.Users.Models;

public sealed record CreateUserDto
{
	public UserId? UserId { get; init; }
	public required string EmailAddress { get; init; }
	public required string Name { get; init; }
	public bool IsActive { get; init; }
}
