using Immediate.Apis.Shared;
using Immediate.Handlers.Shared;
using LinqToDB;
using VsaTemplate.Web.Database;
using VsaTemplate.Web.Features.Users.Models;
using VsaTemplate.Web.Infrastructure.Authorization;

namespace VsaTemplate.Web.Features.Users.Endpoints;

[Handler]
[MapGet("/api/users")]
internal static partial class GetUsers
{
	internal sealed record Query : IAuthorizedRequest
	{
		public static string? Policy => Policies.Admin;
	}

	private static async ValueTask<IEnumerable<User>> HandleAsync(
			Query query,
			DbContext context,
			CancellationToken token) =>
		await context.Users
			.SelectDto()
			.ToListAsync(token);
}
