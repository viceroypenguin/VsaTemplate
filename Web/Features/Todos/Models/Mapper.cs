namespace VsaTemplate.Web.Features.Todos.Models;

public static class Mapper
{
	public static IQueryable<Todo> SelectDto(this IQueryable<Database.Models.Todo> query) =>
		query
			.Select(t => new Todo
			{
				TodoId = t.TodoId,
				Name = t.Name,
				Comment = t.Comment,
				TodoStatus = t.TodoStatusId,
				TodoPriority = t.TodoPriorityId,
				UserId = t.UserId,
			});
}
