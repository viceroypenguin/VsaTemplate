using VsaTemplate.Web.Tests.Fixtures;

namespace VsaTemplate.Web.Tests;

public sealed class TodosTests(ApplicationFactoryFixture fixture)
{
	[Fact]
	public async Task Test1()
	{
		var client = fixture.GetUserClient();

		var todo = await client.CreateTodo(
			new()
			{
				Name = "Test",
				Comment = "This is a test",
				TodoPriority = Client.Todos.Models.TodoPriority.Mid,
				TodoStatus = Client.Todos.Models.TodoStatus.Active,
			},
			TestContext.Current.CancellationToken
		);

		await client.CompleteTodo(
			todo.TodoId,
			completed: true,
			token: TestContext.Current.CancellationToken
		);
	}
}
