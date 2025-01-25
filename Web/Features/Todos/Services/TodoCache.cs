using System.Globalization;
using Immediate.Cache;
using Immediate.Handlers.Shared;
using LinqToDB;
using Microsoft.Extensions.Caching.Memory;
using VsaTemplate.Web.Database;
using VsaTemplate.Web.Features.Todos.Models;

namespace VsaTemplate.Web.Features.Todos.Services;

[RegisterSingleton]
public sealed class TodoCache(
	IMemoryCache memoryCache,
	Owned<IHandler<GetTodo.Request, Todo>> ownedGetTodo
) : ApplicationCacheBase<GetTodo.Request, Todo>(
	memoryCache,
	ownedGetTodo
)
{
	protected override string TransformKey(GetTodo.Request request) =>
		string.Create(CultureInfo.InvariantCulture, $"Todo-{request.TodoId}");
}

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
			.Select(Todo.FromDatabaseEntity)
			.FirstAsync(token);
}
