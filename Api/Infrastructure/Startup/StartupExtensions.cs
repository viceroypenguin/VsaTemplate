using System.Security.Claims;
using Auth0.AspNetCore.Authentication;
using CommunityToolkit.Diagnostics;
using Immediate.Validations.Shared;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.AspNetCore.Mvc;
using VsaTemplate.Api.Features.Users.Models;
using VsaTemplate.Api.Features.Users.Queries;

namespace VsaTemplate.Api.Infrastructure.Startup;

public static class StartupExtensions
{

	public static void AddAuth0(this IServiceCollection services, string? domain, string? clientId)
	{
		Guard.IsNotNull(domain);
		Guard.IsNotNull(clientId);

		_ = services
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

		static async Task ProcessTicket(TicketReceivedContext ctx)
		{
			var user = ctx.Principal;
			if (user is null)
				ThrowHelper.ThrowInvalidOperationException("Got a ticket, but no valid user attached.");

			var auth0Id = user.Claims.FirstOrDefault(c => c.Type is ClaimTypes.NameIdentifier)?.Value;
			if (string.IsNullOrWhiteSpace(auth0Id))
				ThrowHelper.ThrowInvalidOperationException("Completed Auth0 login, but no Auth0 Id present.");

			var emailAddress = user.Claims.FirstOrDefault(c => c.Type is ClaimTypes.Email)?.Value;
			if (string.IsNullOrWhiteSpace(emailAddress))
				ThrowHelper.ThrowInvalidOperationException("Completed Auth0 login, but no email address present.");

			var usersService = ctx.HttpContext.RequestServices.GetRequiredService<GetUserId.Handler>();
			var claims = await usersService.HandleAsync(
				new()
				{
					Auth0UserId = Auth0UserId.From(auth0Id),
					EmailAddress = emailAddress,
				});

			user.AddIdentity(new ClaimsIdentity(claims));
		}
	}

	public static IServiceCollection AddSwagger(this IServiceCollection services) =>
		services.AddSwaggerGen(o =>
		{
			o.CustomSchemaIds(t => t.FullName?.Replace('+', '.'));

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

	public static void ConfigureProblemDetails(ProblemDetailsOptions options) =>
		options.CustomizeProblemDetails = c =>
		{
			if (c.Exception is null)
				return;

			c.ProblemDetails = c.Exception switch
			{
				ValidationException ex => new ValidationProblemDetails(
					ex
						.Errors
						.GroupBy(x => x.PropertyName, StringComparer.OrdinalIgnoreCase)
						.ToDictionary(
							x => x.Key,
							x => x.Select(x => x.ErrorMessage).ToArray(),
							StringComparer.OrdinalIgnoreCase
						)
				)
				{
					Status = StatusCodes.Status400BadRequest,
				},

				UnauthorizedAccessException ex => new()
				{
					Detail = "Access denied.",
					Status = StatusCodes.Status403Forbidden,
				},

				var ex => new ProblemDetails
				{
					Detail = "An error has occurred.",
					Status = StatusCodes.Status500InternalServerError,
				},
			};

			c.HttpContext.Response.StatusCode =
				c.ProblemDetails.Status
				?? StatusCodes.Status500InternalServerError;
		};

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
