using Immediate.Apis.Shared;
using Immediate.Handlers.Shared;
using Immediate.Validations.Shared;
using LinqToDB;
using Microsoft.AspNetCore.Http.HttpResults;
using VsaTemplate.Web.Database;
using VsaTemplate.Web.Features.Todos.Models;
using VsaTemplate.Web.Features.Users.Services;
using VsaTemplate.Web.Infrastructure.Authorization;

namespace VsaTemplate.Web.Features.Todos.Endpoints;

[Handler]
[MapPost("/api/todos/create")]
public static partial class CreateTodo
{
	internal static Created<Response> TransformResult(Response response) =>
		TypedResults.Created($"/api/todos/{response.TodoId}", response);

	[Validate]
	public sealed partial record Command : IAuthorizedRequest, IValidationTarget<Command>
	{
		public static string Policy => Policies.ValidUser;

		[NotEmpty]
		public required string Name { get; init; }

		public string? Comment { get; init; }
		public TodoStatus TodoStatus { get; init; }
		public TodoPriority TodoPriority { get; init; }
	}

	public sealed record Response
	{
		public TodoId TodoId { get; init; }
	}

	private static async ValueTask<Response> HandleAsync(
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

		return new()
		{
			TodoId = (TodoId)id,
		};
	}
}
