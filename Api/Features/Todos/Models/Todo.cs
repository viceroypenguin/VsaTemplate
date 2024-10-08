using Immediate.Validations.Shared;
using VsaTemplate.Api.Features.Todos.Authorization;
using VsaTemplate.Api.Features.Users.Models;

namespace VsaTemplate.Api.Features.Todos.Models;

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

public sealed record Todo : ITodoRequest
{
	public required TodoId TodoId { get; init; }
	public required string Name { get; init; }
	public required string? Comment { get; init; }
	public required TodoStatus TodoStatus { get; init; }
	public required TodoPriority TodoPriority { get; init; }
	public required UserId UserId { get; init; }
}
