using Immediate.Apis.Shared;
using Immediate.Handlers.Shared;
using Immediate.Validations.Shared;
using LinqToDB;
using Microsoft.AspNetCore.Authorization;
using VsaTemplate.Api.Database;
using VsaTemplate.Api.Features.Todos.Authorization;
using VsaTemplate.Api.Features.Todos.Models;
using VsaTemplate.Api.Features.Users.Services;
using VsaTemplate.Api.Infrastructure.Authorization;

namespace VsaTemplate.Api.Features.Todos.Endpoints;

[Handler]
[MapPost("/api/todos/{todoId:int}")]
[Authorize(Policy = Policies.ValidUser)]
public static partial class CompleteTodo
{
	[Validate]
	public sealed partial record Command : ITodoRequest, IValidationTarget<Command>
	{
		public required TodoId TodoId { get; init; }
		public bool Completed { get; init; } = true;
	}

	private static async ValueTask<bool> HandleAsync(
		[AsParameters]
		Command command,
		CurrentUserService currentUserService,
		DbContext context,
		CancellationToken token
	)
	{
		var userId = await currentUserService.GetCurrentUserId();

		var count = await context.Todos
			.Where(t => t.TodoId == command.TodoId)
			.Where(t => t.UserId == userId)
			.UpdateAsync(
				t => new()
				{
					TodoStatusId = command.Completed
						? TodoStatus.Completed
						: TodoStatus.Active,
				},
				token: token
			);

		return count == 1;
	}
}
