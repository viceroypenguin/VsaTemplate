using Refit;
using VsaTemplate.Web.Client.Todos.Models;

namespace VsaTemplate.Web.Client;

public partial interface IWebClient
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
