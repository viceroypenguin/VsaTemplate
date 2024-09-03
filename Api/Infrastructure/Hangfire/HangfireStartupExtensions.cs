using CommunityToolkit.Diagnostics;
using Hangfire;

namespace VsaTemplate.Api.Infrastructure.Hangfire;

public static class HangfireStartupExtensions
{
	public static void AddHangfire(this IServiceCollection services, string? connectionString)
	{
		Guard.IsNotNullOrWhiteSpace(connectionString);

		_ = services.AddHangfire(configuration => configuration
			.UseFilter(new HangfireJobIdEnricher())
			.SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
			.UseSimpleAssemblyNameTypeSerializer()
			.UseRecommendedSerializerSettings()
			.UseSqlServerStorage(
				BuildHangfireConnectionString(connectionString),
				new() { PrepareSchemaIfNecessary = false }
			));

		_ = services.AddHangfireServer();
		_ = services.AddHostedService<HangfireInitializationService>();

		static string BuildHangfireConnectionString(string connectionString)
		{
			var builder = new Microsoft.Data.SqlClient.SqlConnectionStringBuilder(connectionString);
			builder.ApplicationName = builder.ApplicationName.Replace("VsaTemplate", "Hangfire", StringComparison.OrdinalIgnoreCase);
			_ = builder.Remove("MultipleActiveResultSets");
			return builder.ConnectionString;
		}
	}

	public static IApplicationBuilder UseHangfire(this IApplicationBuilder app) =>
		app.UseHangfireDashboard(
			"/hangfire",
			new DashboardOptions
			{
				Authorization =
				[
					new AdminAuthorizationFilter(),
				],
			});
}
