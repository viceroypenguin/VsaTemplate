using Immediate.Apis.Shared;
using Immediate.Handlers.Shared;
using LinqToDB;
using VsaTemplate.Web.Database;
using VsaTemplate.Web.Features.Users.Models;
using VsaTemplate.Web.Infrastructure.Authorization;

namespace VsaTemplate.Web.Features.Users.Endpoints;

[Handler]
[MapGet("/api/users/{UserId}")]
internal static partial class GetUser
{
	internal sealed record Query : IAuthorizedRequest
	{
		public static string? Policy => Policies.Admin;

		public int UserId { get; set; }
	}

	private static async ValueTask<User> HandleAsync(
		Query query,
		DbContext context,
		CancellationToken token
	) => await context.Users
			.Where(u => u.UserId == query.UserId)
			.SelectDto()
			.FirstAsync(token);
}
