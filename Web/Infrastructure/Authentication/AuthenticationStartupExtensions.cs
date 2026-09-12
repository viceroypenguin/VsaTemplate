using System.Globalization;
using System.Security.Claims;
using Auth0.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using VsaTemplate.Web.Features.AccessControl.Models;
using VsaTemplate.Web.Features.AccessControl.Queries;

namespace VsaTemplate.Web.Infrastructure.Authentication;

public static class AuthenticationStartupExtensions
{
	public static void AddWebAuthentication(
		this IServiceCollection services,
		string? domain,
		string? clientId,
		bool useAuth0
	)
	{
		var authBuilder = services
			.AddAuthentication(o =>
			{
				if (useAuth0)
				{
					o.DefaultAuthenticateScheme = ApiKeyAuthenticationHandler.AuthenticationScheme;
					o.DefaultSignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
					o.DefaultChallengeScheme = CookieAuthenticationDefaults.AuthenticationScheme;
				}
				else
				{
					o.DefaultScheme = ApiKeyAuthenticationHandler.AuthenticationScheme;
				}
			})
			.AddScheme<ApiKeyAuthenticationSchemeOptions, ApiKeyAuthenticationHandler>(
				ApiKeyAuthenticationHandler.AuthenticationScheme,
				configureOptions: o => o.UseCookiesBackup = useAuth0
			);

		if (useAuth0)
		{
			ArgumentNullException.ThrowIfNull(domain);
			ArgumentNullException.ThrowIfNull(clientId);

			authBuilder
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
	}

	private static async Task ProcessTicket(TicketReceivedContext ctx)
	{
		var user = ctx.Principal
			?? throw new InvalidOperationException("Got a ticket, but no valid user attached.");

		var auth0Id = user.Claims.FirstOrDefault(c => c.Type is ClaimTypes.NameIdentifier)?.Value;
		if (string.IsNullOrWhiteSpace(auth0Id))
			throw new InvalidOperationException("Completed Auth0 login, but no Auth0 Id present.");

		var emailAddress = user.Claims.FirstOrDefault(c => c.Type is ClaimTypes.Email)?.Value;
		if (string.IsNullOrWhiteSpace(emailAddress))
			throw new InvalidOperationException("Completed Auth0 login, but no email address present.");

		var usersService = ctx.HttpContext.RequestServices.GetRequiredService<GetUserId.Handler>();
		var userId = await usersService.HandleAsync(
			new()
			{
				Auth0UserId = Auth0UserId.From(auth0Id),
				EmailAddress = emailAddress,
			},
			CancellationToken.None
		);

		user.AddIdentity(
			new ClaimsIdentity(
				[
					new Claim("vsa-id", string.Create(CultureInfo.InvariantCulture, $"{userId}")),
				]
			)
		);
	}
}
