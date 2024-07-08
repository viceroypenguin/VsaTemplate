using Immediate.Apis.Shared;
using Immediate.Handlers.Shared;
using Immediate.Validations.Shared;
using LinqToDB;
using Microsoft.AspNetCore.Authorization;
using VsaTemplate.Api.Database;
using VsaTemplate.Api.Features.Shared.Exceptions;
using VsaTemplate.Api.Features.Todos.Authorization;
using VsaTemplate.Api.Features.Todos.Models;
using VsaTemplate.Api.Features.Todos.Services;
using VsaTemplate.Api.Features.Users.Services;
using VsaTemplate.Api.Infrastructure.Authorization;

namespace VsaTemplate.Api.Features.Todos.Endpoints;

[Handler]
[MapPut("/api/todos")]
[Authorize(Policy = Policies.ValidUser)]
public static partial class UpdateTodo
{
	[Validate]
	public sealed partial record Command : ITodoRequest, IValidationTarget<Command>
	{
		public required TodoId TodoId { get; init; }

		[NotEmpty]
		public required string Name { get; init; }

		public required string? Comment { get; init; }
		public required TodoStatus TodoStatus { get; init; }
		public required TodoPriority TodoPriority { get; init; }
	}

	private static async ValueTask HandleAsync(
		Command command,
		DbContext context,
		CurrentUserService currentUserService,
		TodoCache todoCache,
		CancellationToken token
	)
	{
		var cnt = await context.Todos
			.Where(t => t.TodoId == command.TodoId)
			.UpdateAsync(
				t => new()
				{
					Name = command.Name,
					Comment = command.Comment,
					TodoPriorityId = command.TodoPriority,
					TodoStatusId = command.TodoStatus,
				},
				token: token
			);

		if (cnt != 1)
			NotFoundException.ThrowNotFoundException("Todo");

		var userId = await currentUserService.GetCurrentUserId();
		todoCache
			.SetTodo(
				new()
				{
					TodoId = command.TodoId,
					Name = command.Name,
					Comment = command.Comment,
					TodoPriority = command.TodoPriority,
					TodoStatus = command.TodoStatus,
					UserId = userId,
				}
			);
	}
}
