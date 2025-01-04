using System.Text.Json;
using Immediate.Handlers.Shared;
using Immediate.Validations.Shared;
using LinqToDB;
using VsaTemplate.Api.Database;
using VsaTemplate.Api.Features.Users.Models;

namespace VsaTemplate.Api.Features.Users.Queries;

[Handler]
public static partial class GetUserRoles
{
	[Validate]
	public sealed partial record Query : IValidationTarget<Query>
	{
		public required UserId UserId { get; set; }
	}

	private static async ValueTask<IReadOnlyList<string>> HandleAsync(
		Query query,
		DbContext context,
		CancellationToken token)
	{
		return JsonSerializer.Deserialize<IReadOnlyList<string>>(
			await context.Users
				.Where(u => u.UserId == query.UserId)
				.Select(u => u.Roles)
				.FirstAsync(token)
		) ?? [];
	}
}
