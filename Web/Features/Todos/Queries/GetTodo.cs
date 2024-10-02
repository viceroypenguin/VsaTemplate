using Immediate.Handlers.Shared;
using LinqToDB;
using VsaTemplate.Web.Database;
using VsaTemplate.Web.Features.Todos.Models;

namespace VsaTemplate.Web.Features.Todos.Queries;

[Handler]
public static partial class GetTodo
{
	public sealed record Request
	{
		public required TodoId TodoId { get; init; }
	}

	private static async ValueTask<Todo> HandleAsync(
		Request request,
		DbContext context,
		CancellationToken token
	) =>
		await context.Todos
			.Where(t => t.TodoId == request.TodoId)
			.SelectDto()
			.FirstAsync(token);
}
