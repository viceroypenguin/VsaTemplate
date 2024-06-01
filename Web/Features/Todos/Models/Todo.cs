using VsaTemplate.Web.Features.Todos.Authorization;
using VsaTemplate.Web.Features.Users.Models;

namespace VsaTemplate.Web.Features.Todos.Models;

[ValueObject]
public readonly partial record struct TodoId
{
	public static Validation Validate(int value) =>
		value > 0 ? Validation.Ok : Validation.Invalid("Must be greater than zero.");
}

public sealed class Todo : ITodoRequest
{
	public required TodoId TodoId { get; init; }
	public required string Name { get; init; }
	public required string? Comment { get; init; }
	public required TodoStatus TodoStatus { get; init; }
	public required TodoPriority TodoPriority { get; init; }
	public required UserId UserId { get; init; }
}
