using System.Diagnostics;
using System.Text.Json.Serialization;
using DryIoc;
using DryIoc.Microsoft.DependencyInjection;
using Hangfire;
using Immediate.Cache;
using Microsoft.AspNetCore.Mvc;
using Serilog;
using VsaTemplate.Api;
using VsaTemplate.Api.Database;
using VsaTemplate.Api.Infrastructure;
using VsaTemplate.Api.Infrastructure.Authentication;
using VsaTemplate.Api.Infrastructure.Authorization;
using VsaTemplate.Api.Infrastructure.Exceptions;
using VsaTemplate.Api.Infrastructure.Hangfire;
using VsaTemplate.Api.Infrastructure.Logging;
using VsaTemplate.Api.Infrastructure.Startup;

Log.Logger = new LoggerConfiguration()
	.WriteTo.Console(formatProvider: null)
	.CreateBootstrapLogger();

try
{
	var builder = WebApplication.CreateBuilder(args);

	if (builder.Configuration.GetValue("UseSecretsJson", defaultValue: true))
		_ = builder.Configuration.AddJsonFile("secrets.json", optional: true);

	using var container = new Container();
	_ = builder.Host.UseServiceProviderFactory(
		new DryIocServiceProviderFactory(container));

	builder.ConfigureSerilog();

	_ = builder.Services.AddSingleton(typeof(Owned<>));

	builder.Services.AddHangfire(
		builder.Configuration["DbContextOptions:ConnectionString"]
	);

	builder.Services.AddAuthorizationPolicies();

	builder.Services.AddApiAuthentication(
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
	_ = builder.Services.AutoRegisterFromApi();
	_ = builder.Services.AddApiHandlers();
	_ = builder.Services.AddApiBehaviors();
	_ = builder.Services.AddEndpointsApiExplorer();
	_ = builder.Services.AddSwagger();
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
	_ = app.UseSwagger();
	_ = app.UseSwaggerUI();
	_ = app.UseMiddleware<AddRequestIdHeaderMiddleware>();
	_ = app.UseMiddleware<AddRolesMiddleware>();
	_ = app.UseExceptionHandler();
	_ = app.UseRouting();
	_ = app.UseAuthorization();
	_ = app.UseHangfire();
	_ = app.UseAntiforgery();
	_ = app.UseLogging();

	_ = app.UseEndpoints(
		endpoints =>
		{
			_ = endpoints.MapAccountServices();
			_ = endpoints.MapApiEndpoints();
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
