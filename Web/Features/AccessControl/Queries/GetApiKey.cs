using Immediate.Handlers.Shared;
using VsaTemplate.Web.Features.AccessControl.Models;
using VsaTemplate.Web.Features.AccessControl.Services;
using VsaTemplate.Web.Features.Shared.Extensions;

namespace VsaTemplate.Web.Features.AccessControl.Queries;

[Handler]
public sealed partial class GetApiKey(ValidApiKeyCache validApiKeyCache)
{
	public sealed record Request
	{
		public required string ApiKey { get; init; }
	}

	public sealed record Response
	{
		public required bool IsValid { get; init; }
		public required UserId? UserId { get; init; }
	}

	private async ValueTask<Response> HandleAsync(Request request) =>
		await validApiKeyCache.GetValue(new() { ApiKey = request.ApiKey }, cancellationToken: default)
			.Transform(r => new Response()
			{
				IsValid = r.IsValid,
				UserId = r.UserId,
			});
}
