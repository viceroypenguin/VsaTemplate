using Immediate.Handlers.Shared;
using Immediate.Validations.Shared;
using LinqToDB;
using VsaTemplate.Web.Database;
using VsaTemplate.Web.Features.AccessControl.Models;

namespace VsaTemplate.Web.Features.AccessControl.Queries;

[Handler]
public sealed partial class GetUserId(DbContext context)
{
	[Validate]
	public sealed partial record Query : IValidationTarget<Query>
	{
		public required Auth0UserId Auth0UserId { get; set; }
		public required string EmailAddress { get; set; }
	}

	private async ValueTask<UserId> HandleAsync(
		Query query,
		CancellationToken token)
	{
		var merges = await context.Users
			.Merge().Using([new { query.Auth0UserId, query.EmailAddress, }])
			.On((dst, src) => dst.EmailAddress == src.EmailAddress)
			.InsertWhenNotMatched(src =>
				new Database.Models.AccessControl.User
				{
					EmailAddress = src.EmailAddress,
					Auth0UserId = src.Auth0UserId,
					IsActive = true,
					LastLogin = Sql.CurrentTzTimestamp,
				})
			.UpdateWhenMatched((dst, src) =>
				new Database.Models.AccessControl.User
				{
					Auth0UserId = src.Auth0UserId,
					LastLogin = Sql.CurrentTzTimestamp,
				})
			.MergeWithOutputAsync((a, d, i) => new { i.UserId, i.IsActive, })
			.ToListAsync(token);

		if (merges is not [{ } merge])
			throw new InvalidOperationException("Failed saving user");

		if (!merge.IsActive)
			throw new InvalidOperationException("User is not active.");

		return merge.UserId;
	}
}
