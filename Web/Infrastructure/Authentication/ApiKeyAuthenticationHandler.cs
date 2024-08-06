using System.Globalization;
using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.Extensions.Options;
using VsaTemplate.Web.Features.Users.Services;
using VsaTemplate.Web.Infrastructure.Authorization;

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
	ValidApiKeyCache validApiKeyCache
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

		if (await validApiKeyCache.GetValue(new() { ApiKey = key }) is not
			{
				IsValid: true,
				UserId: { } userId
			})
		{
			return AuthenticateResult.NoResult();
		}

		return AuthenticateResult.Success(
			new(
				new([
					new ClaimsIdentity(
						[
							new Claim(Claims.Id, string.Create(CultureInfo.InvariantCulture, $"{userId}")),
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
