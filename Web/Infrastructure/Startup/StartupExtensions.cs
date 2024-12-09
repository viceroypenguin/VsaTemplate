using Auth0.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi.Models;

namespace VsaTemplate.Web.Infrastructure.Startup;

public static class StartupExtensions
{
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
