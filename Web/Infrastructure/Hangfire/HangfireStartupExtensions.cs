using Hangfire;
using VsaTemplate.Web.Database;

namespace VsaTemplate.Web.Infrastructure.Hangfire;

public static class HangfireStartupExtensions
{
	public static void AddHangfire(this WebApplicationBuilder builder)
	{
		var services = builder.Services;

		services
			.AddHangfire((sp, c) => c
				.UseFilter(new HangfireJobIdEnricher())
				.SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
				.UseSimpleAssemblyNameTypeSerializer()
				.UseRecommendedSerializerSettings()
				.UseSqlServerStorage(
					BuildHangfireConnectionString(sp.GetRequiredService<DbContextOptions>().ConnectionString),
					new() { PrepareSchemaIfNecessary = false }
				));

		services.AddHangfireServer();
		services.AddHostedService<HangfireInitializationService>();

		static string BuildHangfireConnectionString(string connectionString)
		{
			var builder = new Microsoft.Data.SqlClient.SqlConnectionStringBuilder(connectionString);
			builder.ApplicationName = builder.ApplicationName.Replace("VsaTemplate", "Hangfire", StringComparison.OrdinalIgnoreCase);
			builder.Remove("MultipleActiveResultSets");
			return builder.ConnectionString;
		}
	}

	public static IApplicationBuilder UseHangfire(this IApplicationBuilder app) =>
		app.UseHangfireDashboard(
			"/hangfire",
			new DashboardOptions
			{
				AsyncAuthorization =
				[
					new AdminAuthorizationFilter(),
				],
			});
}
