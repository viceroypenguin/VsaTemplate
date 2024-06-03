using System.Diagnostics;
using System.Text.Json.Serialization;
using DryIoc;
using DryIoc.Microsoft.DependencyInjection;
using Hangfire;
using Microsoft.AspNetCore.Mvc;
using Serilog;
using VsaTemplate.Web;
using VsaTemplate.Web.Components;
using VsaTemplate.Web.Infrastructure.Authorization;
using VsaTemplate.Web.Infrastructure.Startup;

#pragma warning disable CA1852 // Type can be sealed because it has no subtypes in its containing assembly and is not externally visible

Log.Logger = new LoggerConfiguration()
	.WriteTo.Console(formatProvider: null)
	.CreateBootstrapLogger();

try
{
	var builder = WebApplication.CreateBuilder(args);

	_ = builder.Configuration.AddJsonFile("secrets.json", optional: true);

	using var container = new Container();
	_ = builder.Host.UseServiceProviderFactory(
		new DryIocServiceProviderFactory(container));

	builder.Host.ConfigureSerilog();

	builder.Services.AddHangfire(
		builder.Configuration["DbContextOptions:ConnectionString"]
	);

	builder.Services.AddAuthorizationPolicies();
	builder.Services.AddAuth0(
		builder.Configuration["Auth0:Domain"],
		builder.Configuration["Auth0:ClientId"]
	);

	_ = builder.Services.ConfigureAllOptions();
	_ = builder.Services.Configure<ApiBehaviorOptions>(
		o => o.SuppressInferBindingSourcesForParameters = true
	);

	_ = builder.Services.Configure<Microsoft.AspNetCore.Http.Json.JsonOptions>(
		o => o.SerializerOptions.Converters.Add(new JsonStringEnumConverter())
	);

	_ = builder.Services.AddMemoryCache();
	_ = builder.Services.AddHttpContextAccessor();
	_ = builder.Services.AddCascadingAuthenticationState();
	_ = builder.Services.AutoRegisterFromWeb();
	_ = builder.Services.AddHandlers();
	_ = builder.Services.AddBehaviors();
	_ = builder.Services.AddEndpointsApiExplorer();
	_ = builder.Services.AddSwagger();
	_ = builder.Services.AddAntiforgery();
	_ = builder.Services.AddProblemDetails(StartupExtensions.ConfigureProblemDetails);

	_ = builder.Services
		.AddRazorComponents()
		.AddInteractiveServerComponents();

	_ = builder.Services.AddResponseCompression(
		options => options.EnableForHttps = true
	);

	var app = builder.Build();
	_ = app.InitializeDatabase();

	_ = app.UseHttpsRedirection();
	_ = app.UseStaticFiles();
	_ = app.UseSwagger();
	_ = app.UseSwaggerUI();
	_ = app.UseRouting();
	_ = app.UseAuthorization();
	_ = app.UseHangfire();
	_ = app.UseAntiforgery();
	_ = app.UseLogging();

	_ = app.UseEndpoints(
		endpoints =>
		{
			_ = endpoints.MapAccountServices();
			_ = endpoints.MapWebEndpoints();

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
