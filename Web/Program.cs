using System.Diagnostics;
using System.Text.Json.Serialization;
using DryIoc;
using DryIoc.Microsoft.DependencyInjection;
using Hangfire;
using Immediate.Cache;
using Microsoft.AspNetCore.Mvc;
using Scalar.AspNetCore;
using Serilog;
using VsaTemplate.Web;
using VsaTemplate.Web.Database;
using VsaTemplate.Web.Features.Shared.Layout;
using VsaTemplate.Web.Infrastructure.Authentication;
using VsaTemplate.Web.Infrastructure.Authorization;
using VsaTemplate.Web.Infrastructure.Exceptions;
using VsaTemplate.Web.Infrastructure.Hangfire;
using VsaTemplate.Web.Infrastructure.Logging;
using VsaTemplate.Web.Infrastructure.Middleware;
using VsaTemplate.Web.Infrastructure.Startup;

Log.Logger = new LoggerConfiguration()
	.WriteTo.Console(formatProvider: null)
	.CreateBootstrapLogger();

try
{
	var builder = WebApplication.CreateBuilder(args);

	if (builder.Configuration.GetValue("UseSecretsJson", defaultValue: true))
		_ = builder.Configuration.AddJsonFile("secrets.json", optional: true);

	await using var container = new Container();
	_ = builder.Host.UseServiceProviderFactory(
		new DryIocServiceProviderFactory(container));

	builder.ConfigureSerilog();

	_ = builder.Services.AddSingleton(typeof(Owned<>));

	builder.Services.AddHangfire(
		builder.Configuration.GetValue("Hangfire:Enabled", defaultValue: true),
		builder.Configuration["DbContextOptions:ConnectionString"]
	);

	builder.Services.AddAuthorizationPolicies();

	builder.Services.AddWebAuthentication(
		builder.Configuration["Auth0:Domain"],
		builder.Configuration["Auth0:ClientId"],
		builder.Configuration.GetValue("UseAuth0", defaultValue: true)
	);

	_ = builder.Services.ConfigureAllOptions();
	_ = builder.Services.Configure<ApiBehaviorOptions>(
		o => o.SuppressInferBindingSourcesForParameters = true
	);
	_ = builder.Services.Configure<RouteHandlerOptions>(
		o => o.ThrowOnBadRequest = true
	);

	_ = builder.Services.Configure<Microsoft.AspNetCore.Http.Json.JsonOptions>(
		o => o.SerializerOptions.Converters.Add(new JsonStringEnumConverter())
	);

	_ = builder.Services.AddMemoryCache();
	_ = builder.Services.AddHttpContextAccessor();
	_ = builder.Services.AddCascadingAuthenticationState();
	_ = builder.Services.AutoRegisterFromWeb();
	_ = builder.Services.AddWebHandlers();
	_ = builder.Services.AddWebBehaviors();
	_ = builder.Services.AddEndpointsApiExplorer();
	_ = builder.Services.AddWebOpenApi();
	_ = builder.Services.AddAntiforgery();
	_ = builder.Services.AddProblemDetails(ExceptionStartupExtensions.ConfigureProblemDetails);

	_ = builder.Services
		.AddRazorComponents()
		.AddInteractiveServerComponents();

	_ = builder.Services.AddResponseCompression(
		options => options.EnableForHttps = true
	);

	var app = builder.Build();
	_ = app.InitializeDatabase();

	if (builder.Configuration.GetValue("UseHttpsRedirection", defaultValue: true))
		_ = app.UseHttpsRedirection();

	_ = app.UseStaticFiles();
	_ = app.UseMiddleware<AddRequestIdHeaderMiddleware>();
	_ = app.UseMiddleware<AddRolesMiddleware>();
	_ = app.UseExceptionHandler();
	_ = app.UseRouting();
	_ = app.UseAuthorization();

	if (app.Configuration.GetValue("Hangfire:Enabled", defaultValue: true))
		_ = app.UseHangfire();

	_ = app.UseAntiforgery();
	_ = app.UseLogging();

	_ = app.UseEndpoints(
		endpoints =>
		{
			_ = endpoints.MapOpenApi().CacheOutput();
			_ = endpoints.MapScalarApiReference();

			_ = endpoints.MapAccountServices();

			_ = endpoints
				.MapGroup("")
				.RequireAuthorization()
				.MapWebEndpoints();

			_ = endpoints.MapRazorComponents<App>()
				.AddInteractiveServerRenderMode();
		}
	);

	await app.RunAsync();
}
catch (Exception ex) when (ex is not HostAbortedException)
{
	Log.Fatal(ex, "Unhandled exception");
}
finally
{
	if (new StackTrace().FrameCount == 1)
	{
		Log.Information("Shut down complete");
		await Log.CloseAndFlushAsync();
	}
}

public sealed partial class Program;
