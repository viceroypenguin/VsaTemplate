using Immediate.Apis.Shared;
using Immediate.Handlers.Shared;
using Immediate.Validations.Shared;
using LinqToDB;
using VsaTemplate.Web.Database;
using VsaTemplate.Web.Features.Todos.Authorization;
using VsaTemplate.Web.Features.Todos.Models;
using VsaTemplate.Web.Features.Todos.Services;
using VsaTemplate.Web.Features.Users.Services;
using VsaTemplate.Web.Infrastructure.Authorization;

namespace VsaTemplate.Web.Features.Todos.Endpoints;

[Handler]
[MapPut("/api/todos")]
public static partial class UpdateTodo
{
	[Validate]
	public sealed partial record Command : IAuthorizedRequest, ITodoRequest, IValidationTarget<Command>
	{
		public static string Policy => Policies.ValidUser;

		public required TodoId TodoId { get; init; }

		[NotEmpty]
		public required string Name { get; init; }

		public required string? Comment { get; init; }
		public required TodoStatus TodoStatus { get; init; }
		public required TodoPriority TodoPriority { get; init; }
	}

	private static async ValueTask<bool> HandleAsync(
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
			return false;

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

		return true;
	}
}
