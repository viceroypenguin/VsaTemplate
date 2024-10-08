using Immediate.Handlers.Shared;
using LinqToDB;
using VsaTemplate.Api.Database;
using VsaTemplate.Api.Features.Todos.Models;

namespace VsaTemplate.Api.Features.Todos.Queries;

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
