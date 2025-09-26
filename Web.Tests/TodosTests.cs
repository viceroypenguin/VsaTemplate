using VsaTemplate.Web.Features.Todos.Models;
using VsaTemplate.Web.Tests.Fixtures;

namespace VsaTemplate.Web.Tests;

public sealed class TodosTests(ApplicationFactoryFixture fixture)
{
	[Fact]
	public async Task FullCycleTest()
	{
		var client = fixture.GetUserClient();
		var token = TestContext.Current.CancellationToken;

		var todoResponse = await client.CreateTodo(
			new()
			{
				Name = "Test",
				Comment = "This is a test",
				TodoPriority = TodoPriority.Mid,
				TodoStatus = TodoStatus.Active,
			},
			token
		);

		var todo = new Todo
		{
			TodoId = todoResponse.TodoId,
			Name = "Test",
			Comment = "This is a test",
			TodoPriority = TodoPriority.Mid,
			TodoStatus = TodoStatus.Active,
			UserId = fixture.UserTokenUserId,
		};

		await ValidateTodo(client, todo, showCompleted: false, token);

		await client.UpdateTodo(
			new()
			{
				TodoId = todo.TodoId,
				Name = "Test",
				Comment = "Make a change",
				TodoPriority = TodoPriority.High,
				TodoStatus = TodoStatus.Active,
			},
			token
		);

		await ValidateTodo(
			client,
			todo with { Comment = "Make a change", TodoPriority = TodoPriority.High, },
			showCompleted: false,
			token
		);

		await client.CompleteTodo(
			todo.TodoId,
			completed: true,
			token: token
		);

		await ValidateTodo(
			client,
			todo with { Comment = "Make a change", TodoPriority = TodoPriority.High, TodoStatus = TodoStatus.Completed, },
			showCompleted: true,
			token
		);
	}

	private static async Task ValidateTodo(
		Client.IWebClient client,
		Todo todo,
		bool showCompleted,
		CancellationToken token
	)
	{
		var todos = await client.GetTodos(
			showCompleted,
			token
		);

		var getTodo = Assert.Single(todos, t => t.TodoId == todo.TodoId);
		Assert.Equal(todo, getTodo);
	}
}
