namespace VsaTemplate.Web.Infrastructure.DependencyInjection;

public sealed class Owned<T>(
	IServiceScopeFactory serviceScopeFactory
) where T : class
{
	public interface IOwnedScope : IAsyncDisposable
	{
		public T Value { get; }
	}

	public IOwnedScope GetScope()
	{
		var scope = serviceScopeFactory.CreateAsyncScope();
		return new OwnedScope(
			scope.ServiceProvider.GetRequiredService<T>(),
			scope
		);
	}

	private sealed class OwnedScope(
		T value,
		IAsyncDisposable disposable
	) : IOwnedScope
	{
		public T Value { get; } = value;

		public async ValueTask DisposeAsync()
		{
			await disposable.DisposeAsync();
		}
	}
}
