using System.Security.Claims;
using System.Text.Json;
using CommunityToolkit.Diagnostics;
using Immediate.Handlers.Shared;
using LinqToDB;
using VsaTemplate.Web.Database;
using VsaTemplate.Web.Features.Users.Models;

namespace VsaTemplate.Web.Features.Users.Queries;

[Handler]
public static partial class GetUserClaims
{
	public sealed record Query
	{
		public required Auth0UserId UserId { get; set; }
		public required string EmailAddress { get; set; }
	}

	private static async ValueTask<IReadOnlyList<Claim>> HandleAsync(
		Query query,
		DbContext context,
		CancellationToken token)
	{
		Guard.IsNotNull(query.UserId.Value);
		Guard.IsNotNull(query.EmailAddress);

		var merges = await context.Users
			.Merge().Using([new { query.UserId, query.EmailAddress, }])
			.On((dst, src) => dst.EmailAddress == src.EmailAddress)
			.InsertWhenNotMatched(src =>
				new Database.Models.User
				{
					EmailAddress = src.EmailAddress,
					Auth0UserId = src.UserId,
					IsActive = true,
					LastLogin = Sql.CurrentTzTimestamp,
					Roles = "[]",
				})
			.UpdateWhenMatched((dst, src) =>
				new Database.Models.User
				{
					Auth0UserId = src.UserId,
					LastLogin = Sql.CurrentTzTimestamp,
				})
			.MergeWithOutputAsync((a, d, i) => new { i.UserId, i.IsActive, i.Roles, })
			.ToListAsync(token);

		if (merges.Count != 1)
			return ThrowHelper.ThrowInvalidOperationException<IReadOnlyList<Claim>>("Failed saving user");

		if (!merges[0].IsActive)
			return ThrowHelper.ThrowInvalidOperationException<IReadOnlyList<Claim>>("User is not active.");

		return [
			new Claim(ClaimTypes.NameIdentifier, FormattableString.Invariant($"{merges[0].UserId}")),
			.. JsonSerializer.Deserialize<IReadOnlyList<string>>(merges[0].Roles)!
				.Select(r => new Claim(ClaimTypes.Role, r)),
		];
	}
}
