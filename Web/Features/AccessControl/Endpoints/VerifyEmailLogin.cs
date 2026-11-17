using System.Globalization;
using System.Security.Claims;
using Immediate.Apis.Shared;
using Immediate.Handlers.Shared;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;
using VsaTemplate.Web.Infrastructure.Logging;
using VsaTemplate.Web.Features.AccessControl.Services;
using Immediate.Validations.Shared;

namespace VsaTemplate.Web.Features.AccessControl.Endpoints;

[Handler]
[MapPost("/api/auth/email/verify")]
[AllowAnonymous]
public sealed partial class VerifyEmailLogin(
	EmailLoginService emailLoginService,
	IHttpContextAccessor httpContextAccessor
)
{
	[Validate]
	public sealed partial record Request : ISensitiveRequest, IValidationTarget<Request>
	{
		[NotEmpty]
		public required string EmailAddress { get; init; }

		[NotEmpty]
		public required string SessionId { get; init; }

		[Match(expr: @"\d{6}")]
		public required string Token { get; init; }
	}

	private async ValueTask<NoContent> HandleAsync(Request request, CancellationToken token)
	{
		var user = await emailLoginService.VerifyAsync(request.EmailAddress, request.SessionId, request.Token, token);
		var userId = user.UserId.Value.ToString(CultureInfo.InvariantCulture);

		var identity = new ClaimsIdentity(
			[
				new Claim("vsa-id", userId),
				new Claim(ClaimTypes.NameIdentifier, userId),
				new Claim(ClaimTypes.Name, user.Name ?? user.EmailAddress),
				new Claim(ClaimTypes.Email, user.EmailAddress),
			],
			CookieAuthenticationDefaults.AuthenticationScheme
		);

		await httpContextAccessor.HttpContext!.SignInAsync(
			CookieAuthenticationDefaults.AuthenticationScheme,
			new ClaimsPrincipal(identity),
			new AuthenticationProperties { IsPersistent = false }
		);

		return TypedResults.NoContent();
	}
}
