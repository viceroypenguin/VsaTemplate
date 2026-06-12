using System.Diagnostics;
using System.Text.Json.Serialization;
using Auth0.AspNetCore.Authentication;
using Hangfire;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;
using Scalar.AspNetCore;
using Serilog;
using VsaTemplate.Web;
using VsaTemplate.Web.Database;
using VsaTemplate.Web.Features.Shared.Layout;
using VsaTemplate.Web.Infrastructure.Authentication;
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

	builder.Configuration.AddJsonFile("secrets.json", optional: true);

	builder.ConfigureSerilog();
	builder.AddHangfire();

	builder.Services.AddWebAuthentication(
		builder.Configuration["Auth0:Domain"],
		builder.Configuration["Auth0:ClientId"],
		builder.Configuration.GetValue("UseAuth0", defaultValue: true)
	);

	builder.Services
		.ConfigureWebOptions()
		.AddServices();

	var app = builder.Build();

	await app.InitializeDatabase();

	app.UseStaticFiles();

	app.UseMiddleware<AddRequestIdHeaderMiddleware>();

	app.UseExceptionHandler();
	app.UseRouting();
	app.UseAuthorization();

	app.UseHangfire();

	app.UseAntiforgery();
	app.UseLogging();

	app.MapOpenApi().CacheOutput();
	app.MapScalarApiReference();

	app.MapAccountServices();

	app
		.MapGroup("")
		.RequireAuthorization()
		.MapWebEndpoints();

	app.MapRazorComponents<App>()
		.AddInteractiveServerRenderMode();

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

file static class StartupExtensions
{
	public static IServiceCollection ConfigureWebOptions(this IServiceCollection services)
	{
		return services
			.ConfigureAllOptions()
			.Configure<ApiBehaviorOptions>(
				o => o.SuppressInferBindingSourcesForParameters = true
			)
			.Configure<RouteHandlerOptions>(
				o => o.ThrowOnBadRequest = true
			)
			.Configure<Microsoft.AspNetCore.Http.Json.JsonOptions>(
				o => o.SerializerOptions.Converters.Add(new JsonStringEnumConverter())
			)
			.AddResponseCompression(
				options => options.EnableForHttps = true
			);
	}

	public static IServiceCollection AddServices(this IServiceCollection services)
	{
		return services
			// IH
			.AddWebHandlers()
			.AddWebBehaviors()
			// IC
			.AddWebCaches()
			// IC
			.AddWebServices()

			// General Infra concerns
			.AddWebOpenApi()
			.AddBlazorServices()
			.AddMemoryCache()
			.AddHttpContextAccessor()
			.AddCascadingAuthenticationState()
			.AddEndpointsApiExplorer()
			.AddAntiforgery()
			.AddProblemDetails(ExceptionStartupExtensions.ConfigureProblemDetails);
	}

	private static IServiceCollection AddBlazorServices(this IServiceCollection services)
	{
		services
			.AddRazorComponents()
			.AddInteractiveServerComponents();

		return services;
	}

	private static IServiceCollection AddWebOpenApi(this IServiceCollection services)
	{
		return services.AddOpenApi(o =>
		{
			o.CreateSchemaReferenceId = t =>
				t.Type.IsNested
					? $"{t.Type.DeclaringType!.Name}+{t.Type.Name}"
					: OpenApiOptions.CreateDefaultSchemaReferenceId(t);

			o.MapVogenTypesInWeb();

			o.AddDocumentTransformer(
				(document, context, cancellationToken) =>
				{
					var key = new OpenApiSecurityScheme()
					{
						Scheme = "ApiKey",
						In = ParameterLocation.Header,
						Type = SecuritySchemeType.ApiKey,
						Name = "X-Api-Key",
					};

					document.Components ??= new();
					document.Components.SecuritySchemes ??= new Dictionary<string, IOpenApiSecurityScheme>(StringComparer.Ordinal);
					document.Components.SecuritySchemes["ApiKey"] = key;

					return Task.CompletedTask;
				}
			);

			o.AddOperationTransformer(
				(operation, context, cancellationToken) =>
				{
					if (context.Description.RelativePath?.Split(
							"/",
							count: 3,
							StringSplitOptions.RemoveEmptyEntries
						) is ["api", var name, ..])
					{
						//operation.Tags.Add(new OpenApiTag
						//{
						//	Name = name[..1].ToUpperInvariant() + name[1..],
						//});

						operation.Security = [new() { [new("ApiKey", context.Document)] = [] }];
					}
					else
					{
						operation.Security = [];
					}

					return Task.CompletedTask;
				}
			);
		});
	}

	public static IEndpointRouteBuilder MapAccountServices(this IEndpointRouteBuilder app)
	{
		app
			.MapGet("/Login", async (HttpContext context, string returnUrl = "/") =>
			{
				var authenticationProperties = new LoginAuthenticationPropertiesBuilder()
					.WithRedirectUri(returnUrl)
					.Build();

				await context.ChallengeAsync(Auth0Constants.AuthenticationScheme, authenticationProperties);
			});

		app
			.MapGet("/Logout", async (HttpContext context, string returnUrl = "/") =>
			{
				var authenticationProperties = new LogoutAuthenticationPropertiesBuilder()
					.WithRedirectUri(returnUrl)
					.Build();

				await context.SignOutAsync(Auth0Constants.AuthenticationScheme, authenticationProperties);
				await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
			})
			.RequireAuthorization();

		return app;
	}
}
