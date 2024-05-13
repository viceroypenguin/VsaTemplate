using Immediate.Apis.Shared;
using Immediate.Handlers.Shared;
using LinqToDB;
using VsaTemplate.Web.Database;
using VsaTemplate.Web.Features.Todos.Authorization;
using VsaTemplate.Web.Features.Todos.Models;
using VsaTemplate.Web.Features.Users.Services;
using VsaTemplate.Web.Infrastructure.Authorization;

namespace VsaTemplate.Web.Features.Todos.Endpoints;

[Handler]
[MapPost("/api/todos/{todoId:int}")]
public static partial class CompleteTodo
{
	public sealed record Command : IAuthorizedRequest, ITodoRequest
	{
		public static string Policy => Policies.ValidUser;

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
