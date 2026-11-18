using System.Security.Claims;
using System.Text.Encodings.Web;
using Immediate.Injections.Shared;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.Extensions.Options;
using VsaTemplate.Web.Features.AccessControl.Services;

namespace VsaTemplate.Web.Infrastructure.Authentication;

public sealed class ApiKeyAuthenticationSchemeOptions : AuthenticationSchemeOptions;

[RegisterScoped]
public sealed class ApiKeyAuthenticationHandler(
	IOptionsMonitor<ApiKeyAuthenticationSchemeOptions> options,
	ILoggerFactory logger,
	UrlEncoder encoder,
	ValidApiKeyCache validApiKeyCache,
	TimeProvider timeProvider
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

		return await Context.AuthenticateAsync(CookieAuthenticationDefaults.AuthenticationScheme);
	}

	private async Task<AuthenticateResult> AuthenticateApiKey()
	{
		var apiKey = Context.Request.Headers[HeaderName];

		if (apiKey is not [{ Length: > 0 } key])
			return AuthenticateResult.NoResult();

		GetApiKey.Response result;

		try
		{
			result = await validApiKeyCache.GetValue(new() { ApiKey = key }, Context.RequestAborted);
		}
		catch (OperationCanceledException) when (!Context.RequestAborted.IsCancellationRequested)
		{
			// Cache invalidation can cancel an in-flight lookup.
			return AuthenticateResult.NoResult();
		}

		// Check the issued expiration even when the credential was found in cache.
		if (result is not
			{
				IsValid: true,
				UserId: { } userId,
				OwnerUserId: { } ownerUserId,
				Expiration: { } expiration,
			}
			|| expiration <= timeProvider.GetUtcNow())
		{
			return AuthenticateResult.NoResult();
		}

		return AuthenticateResult.Success(
			new(
				new([
					new ClaimsIdentity(
						[
							new Claim("vsa-id", string.Create(provider: null, $"{userId}")),
							new Claim("owner-id", string.Create(provider: null, $"{ownerUserId}")),
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
