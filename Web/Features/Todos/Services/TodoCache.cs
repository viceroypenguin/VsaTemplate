using System.Globalization;
using Immediate.Cache;
using Immediate.Handlers.Shared;
using Microsoft.Extensions.Caching.Memory;
using VsaTemplate.Web.Features.Todos.Models;
using VsaTemplate.Web.Features.Todos.Queries;

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
