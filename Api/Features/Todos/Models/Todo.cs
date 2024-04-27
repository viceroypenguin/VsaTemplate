using VsaTemplate.Api.Features.Todos.Authorization;
using VsaTemplate.Api.Features.Users.Models;

namespace VsaTemplate.Api.Features.Todos.Models;

[ValueObject]
public readonly partial record struct TodoId;

public sealed class Todo : ITodoRequest
{
	public required TodoId TodoId { get; init; }
	public required string Name { get; init; }
	public required string? Comment { get; init; }
	public required TodoStatus TodoStatus { get; init; }
	public required TodoPriority TodoPriority { get; init; }
	public required UserId UserId { get; init; }
}
