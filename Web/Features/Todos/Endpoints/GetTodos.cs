using Immediate.Apis.Shared;
using Immediate.Handlers.Shared;
using LinqToDB;
using SuperLinq;
using VsaTemplate.Web.Database;
using VsaTemplate.Web.Features.Todos.Models;
using VsaTemplate.Web.Features.Users.Services;
using VsaTemplate.Web.Infrastructure.Authorization;

namespace VsaTemplate.Web.Features.Todos.Endpoints;

[Handler]
[MapGet("/api/todos")]
public static partial class GetTodos
{
	public sealed record Query : IAuthorizedRequest
	{
		public static string Policy => Policies.ValidUser;

		public required bool? ShowCompleted { get; init; }
	}

	private static async ValueTask<IReadOnlyList<Todo>> HandleAsync(
		Query query,
		CurrentUserService currentUserService,
		DbContext context,
		CancellationToken token
	)
	{
		var userId = await currentUserService.GetCurrentUserId();

		var todos = context.Todos
			.Where(t => t.UserId == userId);

		if (query.ShowCompleted is not true)
			todos = todos.Where(t => t.TodoStatusId != TodoStatus.Completed);

		return await todos
			.SelectDto()
			.ToListAsync(token);
	}
}
