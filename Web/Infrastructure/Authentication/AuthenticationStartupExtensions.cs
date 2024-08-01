using System.Security.Claims;
using Auth0.AspNetCore.Authentication;
using CommunityToolkit.Diagnostics;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using VsaTemplate.Web.Features.Users.Models;
using VsaTemplate.Web.Features.Users.Queries;

namespace VsaTemplate.Web.Infrastructure.Authentication;

public static class AuthenticationStartupExtensions
{
	public static void AddWebAuthentication(this IServiceCollection services, string? domain, string? clientId)
	{
		Guard.IsNotNull(domain);
		Guard.IsNotNull(clientId);

		_ = services
			.AddAuthentication(o =>
			{
				o.DefaultAuthenticateScheme = ApiKeyAuthenticationHandler.AuthenticationScheme;
				o.DefaultSignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
				o.DefaultChallengeScheme = CookieAuthenticationDefaults.AuthenticationScheme;
			})
			.AddScheme<AuthenticationSchemeOptions, ApiKeyAuthenticationHandler>(
				ApiKeyAuthenticationHandler.AuthenticationScheme,
				configureOptions: null
			)
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

}
