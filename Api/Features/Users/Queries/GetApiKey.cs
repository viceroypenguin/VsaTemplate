using Immediate.Handlers.Shared;
using LinqToDB;
using VsaTemplate.Api.Database;
using VsaTemplate.Api.Features.Users.Models;

namespace VsaTemplate.Api.Features.Users.Queries;

[Handler]
public static partial class GetApiKey
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

	private static async ValueTask<Response> HandleAsync(
		Request request,
		DbContext context,
		CancellationToken token
	)
	{
		var userId = await context.Users
			.Where(u => u.EmailAddress == request.ApiKey)
			.Where(u => u.ApiKey != null)
			.Select(u => (UserId?)u.UserId)
			.FirstOrDefaultAsync(token);

		return new()
		{
			IsValid = userId != null,
			UserId = userId,
		};
	}
}
