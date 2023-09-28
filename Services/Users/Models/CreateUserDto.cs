namespace VsaTemplate.Users.Models;

public sealed record CreateUserDto
{
	public required string EmailAddress { get; init; }
	public required string Name { get; init; }
	public bool IsActive { get; init; }
}
