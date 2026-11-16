using System.Security.Claims;
using System.Text.Encodings.Web;
using Immediate.Injections.Shared;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.Extensions.Options;
using VsaTemplate.Web.Features.AccessControl.Queries;

namespace VsaTemplate.Web.Infrastructure.Authentication;

public sealed class ApiKeyAuthenticationSchemeOptions : AuthenticationSchemeOptions
{
	public bool UseCookiesBackup { get; set; }
}

[RegisterScoped]
public sealed class ApiKeyAuthenticationHandler(
	IOptionsMonitor<ApiKeyAuthenticationSchemeOptions> options,
	ILoggerFactory logger,
	UrlEncoder encoder,
	GetApiKey.Handler getApiKey
) : AuthenticationHandler<ApiKeyAuthenticationSchemeOptions>(
	options,
	logger,
	encoder
)
{
	public const string AuthenticationScheme = "ApiKey";
	public const string HeaderName = "X-Api-Key";

	protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
	{
		var result = await AuthenticateApiKey();

		if (result.Succeeded)
			return result;

		if (Options.UseCookiesBackup)
			return await Context.AuthenticateAsync(CookieAuthenticationDefaults.AuthenticationScheme);

		return AuthenticateResult.NoResult();
	}

	private async Task<AuthenticateResult> AuthenticateApiKey()
	{
		var apiKey = Context.Request.Headers[HeaderName];

		if (apiKey is not [{ Length: > 0 } key])
			return AuthenticateResult.NoResult();

		// Check account state and expiration on each request. A cached Boolean
		// otherwise keeps disabled or expired keys authenticated.

		if (await getApiKey.HandleAsync(new() { ApiKey = key }, Context.RequestAborted) is not
			{
				IsValid: true,
				UserId: { } userId,
			})
		{
			return AuthenticateResult.NoResult();
		}

		return AuthenticateResult.Success(
			new(
				new([
					new ClaimsIdentity(
						[
							new Claim("vsa-id", string.Create(provider: null, $"{userId}")),
							new Claim(ClaimTypes.NameIdentifier, "Api Key"),
						],
						AuthenticationScheme
					),
				]),
				AuthenticationScheme
			)
		);

	}
}
