using Immediate.Apis.Shared;
using Immediate.Handlers.Shared;
using LinqToDB;
using Microsoft.AspNetCore.Authorization;
using VsaTemplate.Api.Database;
using VsaTemplate.Api.Features.Users.Models;
using VsaTemplate.Api.Infrastructure.Authorization;

namespace VsaTemplate.Api.Features.Users.Endpoints;

[Handler]
[MapGet("/api/users")]
[Authorize(Policy = Policies.Admin)]
public static partial class GetUsers
{
	public sealed record Query;

	private static async ValueTask<IEnumerable<User>> HandleAsync(
		Query _,
		DbContext context,
		CancellationToken token
	) =>
		await context.Users
			.Select(User.FromDatabaseEntity)
			.ToListAsync(token);
}
