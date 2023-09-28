using VsaTemplate.Database;

namespace VsaTemplate.Web.Code;

public class DatabaseInitializationService : IHostedService
{
	private readonly IServiceProvider _serviceProvider;

	public DatabaseInitializationService(IServiceProvider serviceProvider)
	{
		_serviceProvider = serviceProvider;
	}

	public async Task StartAsync(CancellationToken cancellationToken)
	{
		using var scope = _serviceProvider.CreateScope();
		var db = scope.ServiceProvider.GetRequiredService<DbContext>();
		await db.InitializeDatabaseAsync();
	}

	public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
