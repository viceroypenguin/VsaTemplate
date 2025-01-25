using VsaTemplate.Web.Features.Todos.Endpoints;
using VsaTemplate.Web.Features.Todos.Models;

namespace VsaTemplate.Web.Features.Todos.Pages;

public sealed partial class Index : BlazorComponentBase
{
	public sealed class TodoItem
	{
		public required TodoId TodoId { get; set; }
		public required string Name { get; set; }
		public required bool IsComplete { get; set; }
	}

	[InjectScoped]
	private GetTodos.Handler GetTodos { get; init; } = null!;

	[InjectScoped]
	private CreateTodo.Handler CreateTodo { get; init; } = null!;

	[InjectScoped]
	private CompleteTodo.Handler CompleteTodo { get; init; } = null!;

	private List<TodoItem> _todos = null!;
	private bool _showCompleted;
	private string _newTodoText = "";

	protected override Task OnAfterRenderAsync(bool firstRender)
	{
		if (!firstRender)
			return Task.CompletedTask;

		return LoadTodos();
	}

	private async Task LoadTodos(bool newState = false)
	{
		_showCompleted = newState;

		_todos = (await GetTodos.HandleAsync(new() { ShowCompleted = _showCompleted, }))
			.Select(t => new TodoItem
			{
				TodoId = t.TodoId,
				Name = t.Name,
				IsComplete = t.TodoStatus == TodoStatus.Completed,
			})
			.ToList();

		StateHasChanged();
	}

	private async Task AddTodo()
	{
		var todo = await CreateTodo.HandleAsync(
			new()
			{
				Name = _newTodoText,
				TodoPriority = TodoPriority.Mid,
				TodoStatus = TodoStatus.Active,
			}
		);

		_todos.Add(new()
		{
			TodoId = todo.TodoId,
			Name = _newTodoText,
			IsComplete = false,
		});

		_newTodoText = "";
	}

	private async Task Complete(TodoItem t)
	{
		_ = await CompleteTodo.HandleAsync(
			new()
			{
				TodoId = t.TodoId,
				Completed = !t.IsComplete,
			}
		);

		if (!t.IsComplete && !_showCompleted)
		{
			_ = _todos.Remove(t);
			StateHasChanged();
		}
		else
		{
			t.IsComplete = !t.IsComplete;
		}
	}
}
