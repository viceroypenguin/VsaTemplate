using System.Diagnostics;
using System.Text.Json.Serialization;
using Auth0.AspNetCore.Authentication;
using Immediate.Injections.Shared;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.DataProtection.KeyManagement;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi;
using Resend;
using Scalar.AspNetCore;
using Serilog;
using VsaTemplate.Web;
using VsaTemplate.Web.Database;
using VsaTemplate.Web.Features.AccessControl.Services;
using VsaTemplate.Web.Features.AccessControl.Models;
using LinqToDB.Async;
using VsaTemplate.Web.Infrastructure.Authentication;
using VsaTemplate.Web.Infrastructure.Emails;
using VsaTemplate.Web.Infrastructure.Exceptions;
using VsaTemplate.Web.Infrastructure.Logging;
using VsaTemplate.Web.Infrastructure.Middleware;
using Log = Serilog.Log;

Log.Logger = new LoggerConfiguration()
	.WriteTo.Console(formatProvider: null)
	.CreateBootstrapLogger();

try
{
	var builder = WebApplication.CreateBuilder(args);

	builder.Configuration.AddJsonFile("secrets.json", optional: true);

	builder.ConfigureSerilog();

	builder.Services.AddWebAuthentication(
		builder.Configuration["Auth0:Domain"],
		builder.Configuration["Auth0:ClientId"],
		builder.Configuration.GetValue("UseAuth0", defaultValue: true)
	);

	builder.Services.AddWebServices();

	var app = builder.Build();

	await app.InitializeDatabase();

	app.UseStaticFiles();

	app.UseMiddleware<AddRequestIdHeaderMiddleware>();

	app.UseExceptionHandler();
	app.UseRouting();
	app.UseAuthentication();
	app.UseAuthorization();

	app.UseAntiforgery();
	app.Use(async (context, next) =>
	{
		if (context.User.Identity?.IsAuthenticated == true
			&& !context.User.Identities.Any(i => i.AuthenticationType == ApiKeyAuthenticationHandler.AuthenticationScheme)
			&& !HttpMethods.IsGet(context.Request.Method)
			&& !HttpMethods.IsHead(context.Request.Method)
			&& !HttpMethods.IsOptions(context.Request.Method))
		{
			try
			{
				await context.RequestServices.GetRequiredService<IAntiforgery>().ValidateRequestAsync(context);
			}
			catch (AntiforgeryValidationException)
			{
				await Results.Problem(statusCode: StatusCodes.Status400BadRequest, detail: "The request verification token is missing or invalid. Refresh the page and try again.").ExecuteAsync(context);
				return;
			}
		}

		await next(context);
	});
	app.UseLogging();

	app.MapOpenApi().CacheOutput();
	app.MapScalarApiReference();

	app.MapAccountServices();

	app
		.MapWebEndpoints()
		.RequireAuthorization();

	// API typos must retain an HTTP 404 instead of returning the SPA shell.
	app.Map("/api/{**path}", () => Results.NotFound());
	app.MapFallbackToFile("index.html");

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

internal static class StartupExtensions
{
	[RegisterServices]
	public static void ConfigureWebOptions(IServiceCollection services)
	{
		services.Configure<ApiBehaviorOptions>(
			o => o.SuppressInferBindingSourcesForParameters = true
		);

		services.Configure<RouteHandlerOptions>(
			o => o.ThrowOnBadRequest = true
		);

		services.Configure<Microsoft.AspNetCore.Http.Json.JsonOptions>(
			o => o.SerializerOptions.Converters.Add(new JsonStringEnumConverter())
		);

		services.AddResponseCompression(
			options => options.EnableForHttps = true
		);
	}

	[RegisterServices]
	public static void AddEmail(IServiceCollection services)
	{
		services
			.AddOptions<ResendClientOptions>()
			.Configure<IOptions<EmailServiceOptions>>((o, eso) => o.ApiToken = eso.Value.ResendApiToken ?? string.Empty);

		services.AddHttpClient<ResendClient>();
		services.AddTransient<ResendClient>();
	}

	[RegisterServices]
	public static void AddImmediatePlatformHooks(IServiceCollection services)
	{
		// IH
		services.AddWebHandlers();

		// IC
		services.AddWebCaches();
	}

	[RegisterServices]
	public static void AddInfrastructureServices(IServiceCollection services)
	{
		services.AddMemoryCache();
		services.AddHttpContextAccessor();
		services.AddAuthorization();
		services.AddEndpointsApiExplorer();
		services.AddAntiforgery(o => o.HeaderName = "X-CSRF-TOKEN");
		services.AddSingleton(TimeProvider.System);
		services.AddProblemDetails(ExceptionStartupExtensions.ConfigureProblemDetails);
	}

	[RegisterServices]
	public static void AddAddDataProtection(IServiceCollection services)
	{
		services.AddDataProtection();

		services.AddSingleton<IConfigureOptions<KeyManagementOptions>>(
			sp => new ConfigureOptions<KeyManagementOptions>(
				options => options.XmlRepository = sp.GetRequiredService<LinqToDBXmlRepository>()
			)
		);
	}

	[RegisterServices]
	public static void AddWebOpenApi(this IServiceCollection services)
	{
		services.AddOpenApi(o =>
		{
			o.CreateSchemaReferenceId = t =>
				t.Type.IsNested
					? $"{t.Type.DeclaringType!.Name}+{t.Type.Name}"
					: OpenApiOptions.CreateDefaultSchemaReferenceId(t);

			o.MapVogenTypesInVsaTemplate_Web();

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
		app.MapGet("/api/session", async (HttpContext context, CurrentUserService currentUser, DbContext db, IAntiforgery antiforgery) =>
		{
			context.Response.Headers.CacheControl = "no-store";
			var userId = await currentUser.GetCurrentUserId();
			var user = userId is { } id
				? await db.AccessControl.Users.Where(u => u.UserId == id)
					.Select(u => new SessionUser { UserId = u.UserId, Name = u.ApiKey == null ? u.Name : "API key", EmailAddress = u.ApiKey == null ? u.EmailAddress : "" })
					.FirstOrDefaultAsync(context.RequestAborted)
				: null;
			return Results.Ok(new
			{
				IsAuthenticated = user is not null,
				User = user,
				RequestToken = user is null ? null : antiforgery.GetAndStoreTokens(context).RequestToken,
			});
		}).AllowAnonymous();

		app
			.MapGet("/Login", async (HttpContext context, string returnUrl = "/") =>
			{
				var authenticationProperties = new LoginAuthenticationPropertiesBuilder()
					.WithRedirectUri(LocalReturnUrl(returnUrl))
					.Build();

				await context.ChallengeAsync(Auth0Constants.AuthenticationScheme, authenticationProperties);
			});

		app
			.MapPost("/Logout", async context =>
			{
				var authenticationProperties = new LogoutAuthenticationPropertiesBuilder()
					.WithRedirectUri("/")
					.Build();

				await context.SignOutAsync(Auth0Constants.AuthenticationScheme, authenticationProperties);
				await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
			})
			.RequireAuthorization();

		return app;
	}

	private static string LocalReturnUrl(string returnUrl) =>
		returnUrl.StartsWith('/', StringComparison.Ordinal) && !returnUrl.StartsWith("//", StringComparison.Ordinal)
			&& !returnUrl.Contains('\\', StringComparison.Ordinal) ? returnUrl : "/";
}

internal sealed record SessionUser
{
	public required UserId UserId { get; init; }
	public required string? Name { get; init; }
	public required string EmailAddress { get; init; }
}
