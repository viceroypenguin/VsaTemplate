using System.Diagnostics.CodeAnalysis;
using CommunityToolkit.Diagnostics;
using Immediate.Handlers.Shared;
using LinqToDB;
using VsaTemplate.Database;
using VsaTemplate.Web.Features.Users.Models;

namespace VsaTemplate.Web.Features.Users.Handlers;

[Handler]
public static partial class GetOrCreateUserId
{
	[SuppressMessage(
		"VsaTemplate",
		"VSA0001:Api Handler Must Have Authorization",
		Justification = "Only used by ticket management.")]
	public record Query
	{
		public required Auth0UserId UserId { get; set; }
		public required string EmailAddress { get; set; }
	}

	private static async ValueTask<UserId> HandleAsync(
		Query query,
		DbContext context,
		CancellationToken token)
	{
		Guard.IsNotNull(query.UserId.Value);
		Guard.IsNotNull(query.EmailAddress);

		var merges = await context.Users
			.Merge().Using([new { UserId = query.UserId.Value, query.EmailAddress, }])
			.On((dst, src) => dst.EmailAddress == src.EmailAddress)
			.InsertWhenNotMatched(src =>
				new Database.Models.User
				{
					EmailAddress = src.EmailAddress,
					Auth0UserId = src.UserId,
					IsActive = true,
					LastLogin = Sql.CurrentTzTimestamp,
				})
			.UpdateWhenMatched((dst, src) =>
				new Database.Models.User
				{
					Auth0UserId = src.UserId,
					LastLogin = Sql.CurrentTzTimestamp,
				})
			.MergeWithOutputAsync((a, d, i) => new { i.UserId, i.IsActive, })
			.ToListAsync(token);

		if (merges.Count != 1)
			return ThrowHelper.ThrowInvalidOperationException<UserId>("Failed saving user");

		if (!merges[0].IsActive)
			return ThrowHelper.ThrowInvalidOperationException<UserId>("User is not active.");

		return UserId.From(merges[0].UserId);
	}
}
