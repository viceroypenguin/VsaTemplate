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
	[EndpointRegistrationOverride(EndpointRegistration.AsParameters)]
	public sealed record Command : IAuthorizedRequest, ITodoRequest
	{
		public static string Policy => Policies.ValidUser;

		public required TodoId TodoId { get; init; }
	}

	private static async ValueTask<bool> HandleAsync(
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
				t => new() { TodoStatusId = TodoStatus.Completed, },
				token: token
			);

		return count == 1;
	}
}
