using Immediate.Apis.Shared;
using Immediate.Handlers.Shared;
using Immediate.Validations.Shared;
using LinqToDB;
using VsaTemplate.Web.Database;
using VsaTemplate.Web.Features.Users.Models;
using VsaTemplate.Web.Infrastructure.Authorization;

namespace VsaTemplate.Web.Features.Users.Endpoints;

[Handler]
[MapGet("/api/users/{UserId}")]
public static partial class GetUser
{
	[Validate]
	public sealed partial record Query : IAuthorizedRequest, IValidationTarget<Query>
	{
		public static string? Policy => Policies.Admin;

		public UserId UserId { get; set; }
	}

	private static async ValueTask<User> HandleAsync(
		Query query,
		DbContext context,
		CancellationToken token
	) =>
		await context.Users
			.Where(u => u.UserId == query.UserId)
			.SelectDto()
			.FirstAsync(token);
}
