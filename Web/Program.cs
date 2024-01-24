using System.Diagnostics;
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

	_ = builder.Services.AddHostedService<DatabaseInitializationService>();

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

	_ = builder.Services.AddHttpContextAccessor();
	_ = builder.Services.AddCascadingAuthenticationState();
	_ = builder.Services.AutoRegisterFromWeb();
	_ = builder.Services.AddHandlers();
	_ = builder.Services.AddBehaviors();
	_ = builder.Services.AddSwagger();
	_ = builder.Services.AddControllers();
	_ = builder.Services.AddAntiforgery();

	_ = builder.Services
		.AddRazorComponents()
		.AddInteractiveServerComponents();

	_ = builder.Services.AddResponseCompression(
		options => options.EnableForHttps = true
	);

	var app = builder.Build();

	_ = app.UseHttpsRedirection();
	_ = app.UseStaticFiles();
	_ = app.UseSwagger();
	_ = app.UseSwaggerUI();
	_ = app.UseRouting();
	_ = app.UseAuthorization();
	_ = app.UseHangfire();
	_ = app.UseAntiforgery();
	_ = app.UseLogging();

	_ = app.MapAccountServices();
	_ = app.MapControllers();

	_ = app.MapRazorComponents<App>()
		.AddInteractiveServerRenderMode();

	app.Run();
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
		Log.CloseAndFlush();
	}
}
