using System.Globalization;
using System.Security.Claims;
using System.Text.Json;
using CommunityToolkit.Diagnostics;
using Immediate.Handlers.Shared;
using Immediate.Validations.Shared;
using LinqToDB;
using VsaTemplate.Web.Database;
using VsaTemplate.Web.Features.Users.Models;
using VsaTemplate.Web.Infrastructure.Authorization;

namespace VsaTemplate.Web.Features.Users.Queries;

[Handler]
public static partial class GetUserClaims
{
	[Validate]
	public sealed partial record Query : IValidationTarget<Query>
	{
		public required Auth0UserId Auth0UserId { get; set; }
		public required string EmailAddress { get; set; }
	}

	private static async ValueTask<IReadOnlyList<Claim>> HandleAsync(
		Query query,
		DbContext context,
		CancellationToken token)
	{
		var merges = await context.Users
			.Merge().Using([new { query.Auth0UserId, query.EmailAddress, }])
			.On((dst, src) => dst.EmailAddress == src.EmailAddress)
			.InsertWhenNotMatched(src =>
				new Database.Models.User
				{
					EmailAddress = src.EmailAddress,
					Auth0UserId = src.Auth0UserId,
					IsActive = true,
					LastLogin = Sql.CurrentTzTimestamp,
					Roles = "[]",
				})
			.UpdateWhenMatched((dst, src) =>
				new Database.Models.User
				{
					Auth0UserId = src.Auth0UserId,
					LastLogin = Sql.CurrentTzTimestamp,
				})
			.MergeWithOutputAsync((a, d, i) => new { i.UserId, i.IsActive, i.Roles })
			.ToListAsync(token);

		if (merges.Count != 1)
			return ThrowHelper.ThrowInvalidOperationException<IReadOnlyList<Claim>>("Failed saving user");

		if (!merges[0].IsActive)
			return ThrowHelper.ThrowInvalidOperationException<IReadOnlyList<Claim>>("User is not active.");

		return [
			new Claim(Claims.Id, string.Create(CultureInfo.InvariantCulture, $"{merges[0].UserId}")),
			.. JsonSerializer.Deserialize<IReadOnlyList<string>>(merges[0].Roles)!
				.Select(r => new Claim(ClaimTypes.Role, r)),
		];
	}
}
