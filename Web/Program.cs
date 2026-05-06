using System.Diagnostics;
using System.Text.Json.Serialization;
using Auth0.AspNetCore.Authentication;
using Hangfire;
using Immediate.Cache;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi.Extensions;
using Microsoft.OpenApi.Models;
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

Log.Logger = new LoggerConfiguration()
	.WriteTo.Console(formatProvider: null)
	.CreateBootstrapLogger();

try
{
	var builder = WebApplication.CreateBuilder(args);

	_ = builder.Configuration.AddJsonFile("secrets.json", optional: true);

	builder.ConfigureSerilog();
	builder.AddHangfire();

	builder.Services.AddAuthorizationPolicies();

	builder.Services.AddWebAuthentication(
		builder.Configuration["Auth0:Domain"],
		builder.Configuration["Auth0:ClientId"],
		builder.Configuration.GetValue("UseAuth0", defaultValue: true)
	);

	builder.Services
		.ConfigureWebOptions()
		.AddWebServices();

	var app = builder.Build();

	_ = app.InitializeDatabase();

	_ = app.UseStaticFiles();

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

file static class StartupExtensions
{
	public static IServiceCollection ConfigureWebOptions(this IServiceCollection services) =>
		services
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

	public static void AddWebServices(this IServiceCollection services) =>
		services
			// injectio
			.AddWeb()
			// IH
			.AddWebHandlers()
			.AddWebBehaviors()
			// IC
			.AddSingleton(typeof(Owned<>))

			// General Infra concerns
			.AddWebOpenApi()
			.AddMemoryCache()
			.AddHttpContextAccessor()
			.AddCascadingAuthenticationState()
			.AddEndpointsApiExplorer()
			.AddAntiforgery()
			.AddProblemDetails(ExceptionStartupExtensions.ConfigureProblemDetails)

			// blazor
			.AddRazorComponents()
			.AddInteractiveServerComponents();

	public static IServiceCollection AddWebOpenApi(this IServiceCollection services) =>
		services.AddOpenApi(o =>
		{
			o.CreateSchemaReferenceId = t =>
				t.Type.IsNested
					? $"{t.Type.DeclaringType!.Name}+{t.Type.Name}"
					: OpenApiOptions.CreateDefaultSchemaReferenceId(t);

			_ = o.AddSchemaTransformer(
				(schema, context, cancellationToken) =>
				{
					var type = context.JsonTypeInfo.Type;

					foreach (var attribute in type.GetCustomAttributes(inherit: false))
					{
						var underlyingType = attribute switch
						{
							ValueObjectAttribute => typeof(int),

							var a when a.GetType() is
							{
								Namespace: "Vogen",
								Name: "ValueObjectAttribute",
							} t =>
								t.GenericTypeArguments[0],

							_ => null,
						};

						if (underlyingType is null)
							continue;

						schema.Type = OpenApiTypeMapper.MapTypeToOpenApiPrimitiveType(underlyingType).Type;
					}

					return Task.CompletedTask;
				}
			);

			var key = new OpenApiSecurityScheme()
			{
				Reference = new OpenApiReference
				{
					Type = ReferenceType.SecurityScheme,
					Id = "ApiKey",
				},
				In = ParameterLocation.Header,
				Type = SecuritySchemeType.ApiKey,
				Name = "X-Api-Key",
			};

			_ = o.AddDocumentTransformer(
				(document, context, cancellationToken) =>
				{
					document.Components ??= new()
					{
						SecuritySchemes =
						{
							["ApiKey"] = key,
						},
					};

					return Task.CompletedTask;
				}
			);

			_ = o.AddOperationTransformer(
				(operation, context, cancellationToken) =>
				{
					if (context.Description.RelativePath?.Split(
							"/",
							count: 3,
							StringSplitOptions.RemoveEmptyEntries
						) is ["api", var name, ..])
					{
						operation.Tags.Add(new OpenApiTag
						{
							Name = name[..1].ToUpperInvariant() + name[1..],
						});

						operation.Security = [new() { [key] = [] }];
					}
					else
					{
						operation.Security = [];
					}

					return Task.CompletedTask;
				}
			);
		});

	public static IEndpointRouteBuilder MapAccountServices(this IEndpointRouteBuilder app)
	{
		_ = app
			.MapGet("/Login", async (HttpContext context, string returnUrl = "/") =>
			{
				var authenticationProperties = new LoginAuthenticationPropertiesBuilder()
					.WithRedirectUri(returnUrl)
					.Build();

				await context.ChallengeAsync(Auth0Constants.AuthenticationScheme, authenticationProperties);
			});

		_ = app
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
