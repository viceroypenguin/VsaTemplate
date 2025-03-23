using System.Security.Claims;
using Auth0.AspNetCore.Authentication;
using CommunityToolkit.Diagnostics;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using VsaTemplate.Api.Features.Users.Models;
using VsaTemplate.Api.Features.Users.Queries;

namespace VsaTemplate.Api.Infrastructure.Authentication;

public static class AuthenticationStartupExtensions
{
	public static void AddApiAuthentication(
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
			Guard.IsNotNull(domain);
			Guard.IsNotNull(clientId);

			_ = authBuilder
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
			},
			CancellationToken.None
		);

		user.AddIdentity(new ClaimsIdentity(claims));
	}
}
