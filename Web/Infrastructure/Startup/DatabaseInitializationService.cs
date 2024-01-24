using VsaTemplate.Web.Database;

namespace VsaTemplate.Web.Infrastructure.Startup;

public class DatabaseInitializationService(IServiceProvider serviceProvider) : IHostedService
{
	public async Task StartAsync(CancellationToken cancellationToken)
	{
		using var scope = serviceProvider.CreateScope();
		var db = scope.ServiceProvider.GetRequiredService<DbContext>();
		await db.InitializeDatabaseAsync();
	}

	public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
