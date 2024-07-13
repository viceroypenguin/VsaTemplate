using System.Diagnostics;
using Immediate.Handlers.Shared;
using VsaTemplate.Api.Features.Todos.Models;
using VsaTemplate.Api.Features.Todos.Services;
using VsaTemplate.Api.Features.Users.Models;
using VsaTemplate.Api.Features.Users.Services;

namespace VsaTemplate.Api.Features.Todos.Authorization;

public sealed partial class TodoAuthorizationBehavior<TRequest, TResponse>(
	CurrentUserService currentUserService,
	TodoCache todoCache,
	ILogger<TodoAuthorizationBehavior<TRequest, TResponse>> logger
) : Behavior<TRequest, TResponse>
	where TRequest : ITodoRequest
{
	public override async ValueTask<TResponse> HandleAsync(TRequest request, CancellationToken cancellationToken)
	{
		var userId = await currentUserService.GetCurrentUserId();
		var todo = await todoCache.GetTodo(request.TodoId);

		if (todo is not null && todo.UserId != userId)
		{
			LogUnauthorizedAccess(logger, userId, request.TodoId);
			ThrowUnauthorizedAccess(userId, request.TodoId);
		}

		return await Next(request, cancellationToken);
	}

	[LoggerMessage(
		LogLevel.Warning,
		"Unauthorized access: User {UserId}, TodoId {TodoId}"
	)]
	private static partial void LogUnauthorizedAccess(
		ILogger logger,
		UserId userId,
		TodoId todoId
	);

	[StackTraceHidden]
	private static void ThrowUnauthorizedAccess(UserId userId, TodoId todoId) =>
		throw new InvalidOperationException($"Unauthorized access: User {userId}, TodoId {todoId}");
}
