namespace VsaTemplate.Api.Client.Todos.Models;

public sealed record CreateTodoCommand
{
	public required string Name { get; init; }
	public required string? Comment { get; init; }
	public required TodoStatus TodoStatus { get; init; }
	public required TodoPriority TodoPriority { get; init; }
}
