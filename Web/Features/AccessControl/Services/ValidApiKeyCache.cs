using Immediate.Cache.Shared;
using Immediate.Handlers.Shared;
using LinqToDB.Async;
using VsaTemplate.Web.Database;
using VsaTemplate.Web.Features.AccessControl.Models;

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
	public sealed record Request
	{
		public required string ApiKey { get; init; }
	}

	public sealed record Response
	{
		public required bool IsValid { get; init; }
		public required UserId? UserId { get; init; }
	}

	private async ValueTask<Response> HandleAsync(
		Request request,
		CancellationToken token
	)
	{
		var now = timeProvider.GetUtcNow();
		var userId = await context.AccessControl.Users
			.Where(u => u.EmailAddress == request.ApiKey)
			.Where(u => u.IsActive && u.ApiKey != null)
			.Where(u => u.ApiKey!.ExpirationDateTime > now
				&& u.ApiKey.OwnerUser.IsActive && u.ApiKey.OwnerUser.ApiKey == null)
			.Select(u => (UserId?)u.UserId)
			.FirstOrDefaultAsync(token);

		return new()
		{
			IsValid = userId != null,
			UserId = userId,
		};
	}
}
