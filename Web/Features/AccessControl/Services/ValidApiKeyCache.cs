using Immediate.Cache.Shared;
using Immediate.Handlers.Shared;
using LinqToDB.Async;
using VsaTemplate.Web.Database;
using VsaTemplate.Web.Features.AccessControl.Models;
using VsaTemplate.Web.Infrastructure.Logging;

namespace VsaTemplate.Web.Features.AccessControl.Services;

[CacheFor<GetApiKey>]
public sealed partial class ValidApiKeyCache
{
	protected override string TransformKey(GetApiKey.Request request) =>
		$"Valid-ApiKey-{request.ApiKey}";
}

[Handler]
public sealed partial class GetApiKey(DbContext context, TimeProvider timeProvider)
{
	public sealed record Request : ISensitiveRequest
	{
		public required string ApiKey { get; init; }
	}

	public sealed record Response
	{
		public required bool IsValid { get; init; }
		public required UserId? UserId { get; init; }
		public required UserId? OwnerUserId { get; init; }
		public required DateTimeOffset? Expiration { get; init; }
	}

	private async ValueTask<Response> HandleAsync(
		Request request,
		CancellationToken token
	)
	{
		var now = timeProvider.GetUtcNow();

		var apiKey = await
			(
				from u in context.AccessControl.Users
				where u.EmailAddress == request.ApiKey
				where u.IsActive
				let ak = u.ApiKey
				where ak != null
				where ak.ExpirationDateTime > now
				where ak.OwnerUser.IsActive
				where ak.OwnerUser.ApiKey == null
				select new
				{
					u.UserId,
					u.EmailAddress,
					ak.ExpirationDateTime,
					ak.OwnerUserId,
				}
			)
			.FirstOrDefaultAsync(token);

		// Use the same exact credential for database lookups, caching, and revocation.
		if (!string.Equals(apiKey?.EmailAddress, request.ApiKey, StringComparison.Ordinal))
			apiKey = null;

		return new()
		{
			IsValid = apiKey is not null,
			UserId = apiKey?.UserId,
			OwnerUserId = apiKey?.OwnerUserId,
			Expiration = apiKey?.ExpirationDateTime,
		};
	}
}
