using System.Globalization;
using Serilog;
using Serilog.Events;
using Serilog.Exceptions;
using Serilog.Exceptions.Core;
using Serilog.Exceptions.MsSqlServer.Destructurers;
using Serilog.Exceptions.Refit.Destructurers;
using VsaTemplate.Web.Infrastructure.Hangfire;

namespace VsaTemplate.Web.Infrastructure.Logging;

public static class LoggingStartupExtensions
{
	public static void ConfigureSerilog(this WebApplicationBuilder builder) =>
		builder.Host
			.UseSerilog((ctx, lc) => lc
				.MinimumLevel.Information()
				.MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
				.MinimumLevel.Override("Microsoft.Hosting.Lifetime", LogEventLevel.Information)
				.MinimumLevel.Override("System.Net.Http.HttpClient.Refit.Implementation", LogEventLevel.Warning)
				.Enrich.FromLogContext()
				.Enrich.WithProperty("EnvironmentName", builder.Environment.EnvironmentName)
				.Enrich.WithThreadId()
				.Enrich.WithProperty("ExecutionId", Guid.NewGuid())
				.Enrich.WithProperty("Commit", ThisAssembly.Git.Commit)
				.Enrich.With<HangfireJobIdEnricher>()
				.Enrich.WithExceptionDetails(
					new DestructuringOptionsBuilder()
						.WithDefaultDestructurers()
						.WithDestructurers(
						[
							new SqlExceptionDestructurer(),
							new ApiExceptionDestructurer(),
						])
				)
				.WriteTo.Console(formatProvider: CultureInfo.InvariantCulture)
				.WriteTo.Seq(serverUrl: "http://172.16.31.6:5341/"));

	public static IApplicationBuilder UseLogging(this IApplicationBuilder app) =>
		app.UseSerilogRequestLogging(o =>
		{
			o.GetLevel = static (httpContext, _, _) =>
				httpContext.Response.StatusCode >= 500 ? LogEventLevel.Error :
				httpContext.Request.Path.StartsWithSegments(new("/api"), StringComparison.OrdinalIgnoreCase) ? LogEventLevel.Verbose :
				LogEventLevel.Information;

			o.EnrichDiagnosticContext = static (diagnosticContext, httpContext) =>
			{
				diagnosticContext.Set("User", httpContext.User?.Identity?.Name);
				diagnosticContext.Set("RemoteIP", httpContext.Connection.RemoteIpAddress);
				diagnosticContext.Set("ConnectingIP", httpContext.Request.Headers["CF-Connecting-IP"]);
			};
		});
}
