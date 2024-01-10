using Immediate.Handlers.Shared;
using LinqToDB;
using VsaTemplate.Database;
using VsaTemplate.Web.Features.Users.Models;

namespace VsaTemplate.Web.Features.Users.Handlers;

[Handler]
public static partial class GetUsers
{
	public record Query;

	private static async Task<IEnumerable<User>> HandleAsync(
			Query query,
			DbContext context,
			CancellationToken token) =>
		await context.Users
			.SelectDto()
			.ToListAsync(token);
}
