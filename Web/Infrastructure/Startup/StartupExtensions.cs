using System.Reflection;
using System.Security.Claims;
using Auth0.AspNetCore.Authentication;
using CommunityToolkit.Diagnostics;
using Hangfire;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Serilog;
using Serilog.Events;
using Serilog.Exceptions;
using Serilog.Exceptions.Core;
using Serilog.Exceptions.Destructurers;
using Serilog.Exceptions.MsSqlServer.Destructurers;
using Serilog.Exceptions.Refit.Destructurers;
using VsaTemplate.Web.Features.Users.Handlers;
using VsaTemplate.Web.Features.Users.Models;
using VsaTemplate.Web.Infrastructure.Hangfire;

namespace VsaTemplate.Web.Infrastructure.Startup;

public static class StartupExtensions
{
	public static void ConfigureSerilog(this IHostBuilder host) =>
		host
			.UseSerilog((ctx, lc) => lc
			.MinimumLevel.Information()
			.MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
			.MinimumLevel.Override("Microsoft.Hosting.Lifetime", LogEventLevel.Information)
			.MinimumLevel.Override("System.Net.Http.HttpClient.Refit.Implementation", LogEventLevel.Warning)
			.Enrich.FromLogContext()
			.Enrich.WithEnvironmentName()
			.Enrich.WithThreadId()
			.Enrich.WithProperty("ExecutionId", Guid.NewGuid())
			.Enrich.WithProperty("Commit", ThisAssembly.Git.Commit)
			.Enrich.WithExceptionDetails(new DestructuringOptionsBuilder()
				.WithDefaultDestructurers()
				.WithDestructurers(new IExceptionDestructurer[]
				{
					new SqlExceptionDestructurer(),
					new ApiExceptionDestructurer(),
				}))
			.WriteTo.Console()
			.WriteTo.Seq(serverUrl: "http://172.16.31.6:5341/"));

	public static void AddHangfire(this IServiceCollection services, string? connectionString)
	{
		Guard.IsNotNullOrWhiteSpace(connectionString);

		services.AddHangfire(configuration => configuration
			.UseFilter(new HangfireJobIdEnricher())
			.SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
			.UseSimpleAssemblyNameTypeSerializer()
			.UseRecommendedSerializerSettings()
			.UseSqlServerStorage(
				BuildHangfireConnectionString(connectionString)
			));

		services.AddHangfireServer();
		services.AddHostedService<HangfireInitializationService>();
	}

	private static string BuildHangfireConnectionString(string connectionString)
	{
		var builder = new Microsoft.Data.SqlClient.SqlConnectionStringBuilder(connectionString);
		builder.ApplicationName = builder.ApplicationName.Replace("VsaTemplate", "Hangfire", StringComparison.OrdinalIgnoreCase);
		builder.Remove("MultipleActiveResultSets");
		return builder.ConnectionString;
	}

	public static void AddAuth0(this IServiceCollection services, string? domain, string? clientId)
	{
		Guard.IsNotNull(domain);
		Guard.IsNotNull(clientId);

		services
			.AddAuth0WebAppAuthentication(o =>
			{
				o.Domain = domain;
				o.ClientId = clientId;
				o.Scope = "openid profile email";

				o.OpenIdConnectEvents = new()
				{
					OnTicketReceived = ProcessTicket,
				};
			});
	}

	private static async Task ProcessTicket(TicketReceivedContext ctx)
	{
		var user = ctx.Principal;
		if (user is null)
			ThrowHelper.ThrowInvalidOperationException("Got a ticket, but no valid user attached.");

		var auth0Id = user.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
		if (string.IsNullOrWhiteSpace(auth0Id))
			ThrowHelper.ThrowInvalidOperationException("Completed Auth0 login, but no Auth0 Id present.");

		var emailAddress = user.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;
		if (string.IsNullOrWhiteSpace(emailAddress))
			ThrowHelper.ThrowInvalidOperationException("Completed Auth0 login, but no email address present.");

		var usersService = ctx.HttpContext.RequestServices.GetRequiredService<GetUserClaims.Handler>();
		var claims = await usersService.HandleAsync(
			new()
			{
				UserId = Auth0UserId.From(auth0Id),
				EmailAddress = emailAddress,
			});

		user.AddIdentity(new ClaimsIdentity(claims));
	}

	public static void AddSwagger(this IServiceCollection services) =>
		services.AddSwaggerGen(o =>
		{
			o.CustomSchemaIds(t => t.FullName?.Replace("+", ".", StringComparison.Ordinal));
			o.TagActionsBy(api =>
			{
				if (api.ActionDescriptor is not ControllerActionDescriptor descriptor)
					return ThrowHelper.ThrowInvalidOperationException<IList<string>>("Unable to determine tag for endpoint.");

				var route = descriptor.ControllerTypeInfo
					.GetCustomAttribute<RouteAttribute>();
				if (route?.Template is not { } template)
					return ThrowHelper.ThrowInvalidOperationException<IList<string>>("Unable to determine tag for endpoint.");

				var url = template["/api/".Length..];
				return [url[..1].ToUpperInvariant() + url[1..]];
			});
		});

	public static void UseHangfire(this IApplicationBuilder app) =>
		app.UseHangfireDashboard(
		"/hangfire",
		new DashboardOptions
		{
			Authorization =
			[
				new AdminAuthorizationFilter(),
			],
		});

	public static void UseLogging(this IApplicationBuilder app) =>
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

	public static void MapAccountServices(this IEndpointRouteBuilder app)
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
	}
}
