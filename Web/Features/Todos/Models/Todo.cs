using System.Linq.Expressions;
using Immediate.Validations.Shared;
using VsaTemplate.Web.Features.Users.Models;

namespace VsaTemplate.Web.Features.Todos.Models;

[ValueObject]
[Validate]
public readonly partial record struct TodoId : IValidationTarget<TodoId>
{
	private static void AdditionalValidations(ValidationResult errors, TodoId userId)
	{
		errors.Add(
			() => GreaterThanAttribute.ValidateProperty(userId.Value, 0),
			"Id must be greater than zero."
		);
	}
}

public sealed record Todo
{
	public required TodoId TodoId { get; init; }
	public required string Name { get; init; }
	public required string? Comment { get; init; }
	public required TodoStatus TodoStatus { get; init; }
	public required TodoPriority TodoPriority { get; init; }
	public required UserId UserId { get; init; }

	public static readonly Expression<Func<Database.Models.Todo, Todo>> FromDatabaseEntity =
		t => new Todo
		{
			TodoId = t.TodoId,
			Name = t.Name,
			Comment = t.Comment,
			TodoStatus = t.TodoStatusId,
			TodoPriority = t.TodoPriorityId,
			UserId = t.UserId,
		};
}
