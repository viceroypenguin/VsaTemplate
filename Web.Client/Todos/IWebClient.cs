using Refit;
using VsaTemplate.Web.Features.Todos.Endpoints;
using VsaTemplate.Web.Features.Todos.Models;

namespace VsaTemplate.Web.Client;

public partial interface IWebClient
{
	[Get("/api/todos")]
	Task<IReadOnlyList<Todo>> GetTodos(bool? showCompleted = default, CancellationToken token = default);

	[Post("/api/todos/create")]
	Task<Todo> CreateTodo([Body] CreateTodo.Command command, CancellationToken token = default);

	[Put("/api/todos")]
	Task UpdateTodo([Body] UpdateTodo.Command command, CancellationToken token = default);

	[Post("/api/todos/{todoId}")]
	Task CompleteTodo(TodoId todoId, bool completed, CancellationToken token = default);
}
