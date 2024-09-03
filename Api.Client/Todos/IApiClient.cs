using Refit;
using VsaTemplate.Api.Client.Todos.Models;

namespace VsaTemplate.Api.Client;

public partial interface IApiClient
{
	[Get("/api/todos")]
	Task<IReadOnlyList<Todo>> GetTodos(bool? showCompleted = default, CancellationToken token = default);

	[Post("/api/todos/create")]
	Task<Todo> CreateTodo([Body] CreateTodoCommand command, CancellationToken token = default);

	[Put("/api/todos")]
	Task UpdateTodo([Body] UpdateTodoCommand command, CancellationToken token = default);

	[Post("/api/todos/{todoId}")]
	Task CompleteTodo(TodoId todoId, bool completed, CancellationToken token = default);
}
