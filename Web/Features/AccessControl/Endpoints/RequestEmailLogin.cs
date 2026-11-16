using Immediate.Apis.Shared;
using Immediate.Handlers.Shared;
using Microsoft.AspNetCore.Authorization;
using VsaTemplate.Web.Infrastructure.Logging;
using VsaTemplate.Web.Features.AccessControl.Services;

namespace VsaTemplate.Web.Features.AccessControl.Endpoints;

[Handler]
[MapPost("/api/auth/email/request")]
[AllowAnonymous]
public sealed partial class RequestEmailLogin(EmailLoginService emailLoginService)
{
	public sealed record Request : ISensitiveRequest
	{
		public required string EmailAddress { get; init; }
		public required string SessionId { get; init; }
	}

	public sealed record Response
	{
		public required DateTimeOffset ExpiresAt { get; init; }
	}

	private async ValueTask<Response> HandleAsync(Request request, CancellationToken token) => new()
	{
		ExpiresAt = await emailLoginService.RequestAsync(request.EmailAddress, request.SessionId, token),
	};
}
