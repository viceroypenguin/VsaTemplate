using Immediate.Apis.Shared;
using Immediate.Handlers.Shared;
using LinqToDB;
using VsaTemplate.Web.Database;
using VsaTemplate.Web.Features.Users.Models;
using VsaTemplate.Web.Infrastructure.Authorization;

namespace VsaTemplate.Web.Features.Users.Endpoints;

[Handler]
[MapGet("/api/users/active")]
public static partial class GetActiveUsers
{
	public sealed record Query : IAuthorizedRequest
	{
		public static string? Policy => Policies.Admin;
	}

	private static async ValueTask<IEnumerable<User>> HandleAsync(
		Query _,
		DbContext context,
		CancellationToken token
	) =>
		await context.Users
			.Where(u => u.IsActive)
			.Select(User.FromDatabaseEntity)
			.ToListAsync(token);
}
