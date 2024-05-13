using Immediate.Apis.Shared;
using Immediate.Handlers.Shared;
using LinqToDB;
using Microsoft.AspNetCore.Authorization;
using VsaTemplate.Api.Database;
using VsaTemplate.Api.Features.Todos.Models;
using VsaTemplate.Api.Features.Users.Services;
using VsaTemplate.Api.Infrastructure.Authorization;

namespace VsaTemplate.Api.Features.Todos.Endpoints;

[Handler]
[MapPost("/api/todos/create")]
[Authorize(Policy = Policies.ValidUser)]
public static partial class CreateTodo
{
	public sealed record Command
	{
		public required string Name { get; init; }
		public string? Comment { get; init; }
		public TodoStatus TodoStatus { get; init; } = TodoStatus.Active;
		public TodoPriority TodoPriority { get; init; } = TodoPriority.Mid;
	}

	private static async ValueTask<Todo> HandleAsync(
		Command command,
		DbContext context,
		CurrentUserService currentUserService,
		CancellationToken token
	)
	{
		var userId = await currentUserService.GetCurrentUserId();

		var id = await context.InsertWithInt32IdentityAsync(
			new Database.Models.Todo()
			{
				Name = command.Name,
				Comment = command.Comment,
				TodoPriorityId = command.TodoPriority,
				TodoStatusId = command.TodoStatus,
				UserId = userId,
			},
			token: token
		);

		return new Todo
		{
			TodoId = (TodoId)id,
			Name = command.Name,
			Comment = command.Comment,
			TodoPriority = command.TodoPriority,
			TodoStatus = command.TodoStatus,
			UserId = userId,
		};
	}
}
