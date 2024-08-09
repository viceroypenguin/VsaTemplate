using Auth0.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.OpenApi.Models;
using VsaTemplate.Web.Infrastructure.Authentication;

namespace VsaTemplate.Web.Infrastructure.Startup;

public static class StartupExtensions
{
	public static IServiceCollection AddSwagger(this IServiceCollection services) =>
		services.AddSwaggerGen(o =>
		{
			_ = o.MapVogenTypes();

			o.CustomSchemaIds(t => t.FullName?.Replace('+', '.'));

			o.AddSecurityDefinition(
				"ApiKey",
				new()
				{
					In = ParameterLocation.Header,
					Name = ApiKeyAuthenticationHandler.HeaderName,
					Type = SecuritySchemeType.ApiKey,
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
			};

			o.AddSecurityRequirement(
				new()
				{
					{ key, [] },
				}
			);

			o.DocInclusionPredicate((_, api) =>
				api.ActionDescriptor
					.EndpointMetadata
					.OfType<IRouteDiagnosticsMetadata>()
					.FirstOrDefault()
					is { Route: var route }
				&& route.StartsWith("/api", StringComparison.OrdinalIgnoreCase)
			);

			o.TagActionsBy(api =>
			{
				var routeMetadata = api.ActionDescriptor
					.EndpointMetadata
					.OfType<IRouteDiagnosticsMetadata>()
					.FirstOrDefault();

				if (routeMetadata is not { Route: var route })
					throw new InvalidOperationException("Unable to determine tag for endpoint.");

				var splits = route["/api/".Length..].Split('/');
				if (splits is not [{ } tag, ..]
					|| string.IsNullOrWhiteSpace(tag))
				{
					throw new InvalidOperationException("Unable to determine tag for endpoint.");
				}

				return [tag[..1].ToUpperInvariant() + tag[1..]];
			});
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
