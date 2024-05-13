using Immediate.Apis.Shared;
using Immediate.Handlers.Shared;
using LinqToDB;
using Microsoft.AspNetCore.Authorization;
using VsaTemplate.Api.Database;
using VsaTemplate.Api.Features.Users.Models;
using VsaTemplate.Api.Infrastructure.Authorization;

namespace VsaTemplate.Api.Features.Users.Endpoints;

[Handler]
[MapGet("/api/users/active")]
[Authorize(Policy = Policies.Admin)]
public static partial class GetActiveUsers
{
	public sealed record Query;

	private static async ValueTask<IEnumerable<User>> HandleAsync(
			Query query,
			DbContext context,
			CancellationToken token) =>
		await context.Users
			.Where(u => u.IsActive)
			.SelectDto()
			.ToListAsync(token);
}
