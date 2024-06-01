using System.Text.Json;
using CommunityToolkit.Diagnostics;
using Immediate.Apis.Shared;
using Immediate.Handlers.Shared;
using Immediate.Validations.Shared;
using LinqToDB;
using VsaTemplate.Web.Database;
using VsaTemplate.Web.Features.Users.Models;
using VsaTemplate.Web.Infrastructure.Authorization;

namespace VsaTemplate.Web.Features.Users.Endpoints;

[Handler]
[MapPut("/api/users/active")]
public static partial class UpdateUser
{
	[Validate]
	public sealed partial record Query : IAuthorizedRequest, IValidationTarget<Query>
	{
		public static string? Policy => Policies.Admin;

		public required UserId UserId { get; set; }

		[NotEmpty]
		public required string EmailAddress { get; init; }

		[NotEmpty]
		public required string Name { get; init; }

		public required bool IsActive { get; init; }
		public required IReadOnlyList<string> Roles { get; init; }
	}

	private static async ValueTask<User> HandleAsync(
		Query query,
		DbContext context,
		CancellationToken token)
	{
		var rows = await context.Users
			.Where(u => u.UserId == query.UserId)
			.UpdateWithOutputAsync(
				u => new()
				{
					Name = query.Name,
					EmailAddress = query.EmailAddress,
					IsActive = query.IsActive,
					Roles = JsonSerializer.Serialize(query.Roles, JsonSerializerOptions.Default),
				},
				(del, ins) => ins,
				token);

		if (rows.Length != 1)
			return ThrowHelper.ThrowInvalidOperationException<User>("Failed saving user");

		return rows[0].ToDto();
	}
}
