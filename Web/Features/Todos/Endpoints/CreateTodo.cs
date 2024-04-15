using Immediate.Apis.Shared;
using Immediate.Handlers.Shared;
using LinqToDB;
using VsaTemplate.Web.Database;
using VsaTemplate.Web.Features.Todos.Models;
using VsaTemplate.Web.Features.Users.Services;
using VsaTemplate.Web.Infrastructure.Authorization;

namespace VsaTemplate.Web.Features.Todos.Endpoints;

[Handler]
[MapPost("/api/todos/create")]
public static partial class CreateTodo
{
	public sealed record Command : IAuthorizedRequest
	{
		public static string Policy => Policies.ValidUser;

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
