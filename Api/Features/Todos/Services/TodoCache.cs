using System.Globalization;
using LinqToDB;
using Microsoft.Extensions.Caching.Memory;
using VsaTemplate.Api.Database;
using VsaTemplate.Api.Features.Todos.Models;
using VsaTemplate.Api.Infrastructure.DependencyInjection;

namespace VsaTemplate.Api.Features.Todos.Services;

[RegisterSingleton]
public sealed class TodoCache(
	IMemoryCache cache,
	Owned<DbContext> ownedContext
)
{
	private static string TransformKey(TodoId todoId) =>
		string.Create(CultureInfo.InvariantCulture, $"Todo-{todoId}");

	public async ValueTask<Todo?> GetTodo(TodoId todoId) =>
		await cache.GetOrCreateAsync(
			TransformKey(todoId),
			async entry =>
			{
				entry.SlidingExpiration = TimeSpan.FromMinutes(5);

				await using var scope = ownedContext.GetScope();
				return await scope.Value.Todos
					.Where(t => t.TodoId == todoId)
					.SelectDto()
					.FirstOrDefaultAsync();
			}
		);

	public void SetTodo(Todo todo)
	{
		var key = TransformKey(todo.TodoId);
		_ = cache.Set(key, todo, new MemoryCacheEntryOptions { SlidingExpiration = TimeSpan.FromMinutes(5) });
	}
}
