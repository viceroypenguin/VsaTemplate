using System.Diagnostics;
using DryIoc;
using DryIoc.Microsoft.DependencyInjection;
using Hangfire;
using Microsoft.AspNetCore.Mvc;
using Serilog;
using VsaTemplate.Web;
using VsaTemplate.Web.Components;
using VsaTemplate.Web.Infrastructure.Startup;

#pragma warning disable CA1852 // Type can be sealed because it has no subtypes in its containing assembly and is not externally visible

Log.Logger = new LoggerConfiguration()
	.WriteTo.Console(formatProvider: null)
	.CreateBootstrapLogger();

try
{
	var builder = WebApplication.CreateBuilder(args);

	builder.Configuration.AddJsonFile("secrets.json", optional: true);

	using var container = new Container();
	builder.Host.UseServiceProviderFactory(
		new DryIocServiceProviderFactory(container));

	builder.Host.ConfigureSerilog();

	builder.Services.AddHostedService<DatabaseInitializationService>();

	builder.Services.AddHangfire(
		builder.Configuration["DbContextOptions:ConnectionString"]
	);

	builder.Services.AddAuth0(
		builder.Configuration["Auth0:Domain"],
		builder.Configuration["Auth0:ClientId"]
	);

	builder.Services.ConfigureAllOptions();
	builder.Services.Configure<ApiBehaviorOptions>(o =>
	{
		o.SuppressInferBindingSourcesForParameters = true;
	});

	builder.Services.AddHttpContextAccessor();
	builder.Services.AddCascadingAuthenticationState();
	builder.Services.AutoRegisterFromWeb();
	builder.Services.AddHandlers();
	builder.Services.AddBehaviors();
	builder.Services.AddSwagger();
	builder.Services.AddControllers();
	builder.Services.AddAntiforgery();

	builder.Services
		.AddRazorComponents()
		.AddInteractiveServerComponents();

	builder.Services.AddResponseCompression(options =>
	{
		options.EnableForHttps = true;
	});

	var app = builder.Build();

	app.UseHttpsRedirection();
	app.UseStaticFiles();
	app.UseSwagger();
	app.UseSwaggerUI();
	app.UseRouting();
	app.UseAuthorization();
	app.UseHangfire();
	app.UseAntiforgery();
	app.UseLogging();

	app.MapAccountServices();
	app.MapControllers();

	app.MapRazorComponents<App>()
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
