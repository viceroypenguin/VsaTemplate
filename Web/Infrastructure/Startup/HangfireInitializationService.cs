using System.Reflection;
using CommunityToolkit.Diagnostics;
using Hangfire;
using VsaTemplate.Web.Infrastructure.Jobs;

namespace VsaTemplate.Web.Infrastructure.Startup;

public class HangfireInitializationService : IHostedService
{
	private readonly IServiceProvider _serviceProvider;

	public HangfireInitializationService(IServiceProvider serviceProvider)
	{
		_serviceProvider = serviceProvider;
	}

	public Task StartAsync(CancellationToken cancellationToken)
	{
		using var scope = _serviceProvider.CreateScope();
		var jobManager = scope.ServiceProvider.GetRequiredService<IRecurringJobManager>();

		var initializeJobMethod = GetType().GetMethod(nameof(InitializeJob), BindingFlags.NonPublic | BindingFlags.Static);
		Guard.IsNotNull(initializeJobMethod);

		foreach (var c in typeof(IRecurringJob).Assembly.GetTypes())
		{
			if (!c.IsClass)
				continue;

			if (c.GetCustomAttribute<RecurringJobAttribute>() is not { } rja)
				continue;

			var func = initializeJobMethod.MakeGenericMethod(c);
			func.Invoke(null, new object[] { jobManager, rja.RecurringJobId, rja.Cron, rja.TimeZone, rja.Queue, });
		}

		return Task.CompletedTask;
	}

	private static void InitializeJob<T>(IRecurringJobManager jobManager, string jobid, string cron, string timezone, string queue)
		where T : class, IRecurringJob
	{
		jobManager.AddOrUpdate<T>(
			jobid,
			queue,
			t => t.Execute(CancellationToken.None),
			cron,
			new RecurringJobOptions
			{
				TimeZone = TimeZoneInfo.FindSystemTimeZoneById(timezone),
			});
	}

	public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
