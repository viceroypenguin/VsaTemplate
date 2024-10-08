using VsaTemplate.Web.Features.Todos.Models;
using VsaTemplate.Web.Tests.Fixtures;

namespace VsaTemplate.Web.Tests;

public sealed class TodosTests(ApplicationFactoryFixture fixture)
{
	[Fact]
	public async Task FullCycleTest()
	{
		var client = fixture.GetUserClient();

		var todo = await client.CreateTodo(
			new()
			{
				Name = "Test",
				Comment = "This is a test",
				TodoPriority = TodoPriority.Mid,
				TodoStatus = TodoStatus.Active,
			},
			TestContext.Current.CancellationToken
		);

		await ValidateTodo(client, todo, showCompleted: false);

		await client.UpdateTodo(
			new()
			{
				TodoId = todo.TodoId,
				Name = "Test",
				Comment = "Make a change",
				TodoPriority = TodoPriority.High,
				TodoStatus = TodoStatus.Active,
			},
			TestContext.Current.CancellationToken
		);

		await ValidateTodo(
			client,
			todo with { Comment = "Make a change", TodoPriority = TodoPriority.High, },
			showCompleted: false
		);

		await client.CompleteTodo(
			todo.TodoId,
			completed: true,
			token: TestContext.Current.CancellationToken
		);

		await ValidateTodo(
			client,
			todo with { Comment = "Make a change", TodoPriority = TodoPriority.High, TodoStatus = TodoStatus.Completed, },
			showCompleted: true
		);
	}

	private static async Task ValidateTodo(Client.IWebClient client, Todo todo, bool showCompleted)
	{
		var todos = await client.GetTodos(
			showCompleted,
			TestContext.Current.CancellationToken
		);

		var getTodo = Assert.Single(todos.Where(t => t.TodoId == todo.TodoId));
		Assert.Equal(todo, getTodo);
	}
}
