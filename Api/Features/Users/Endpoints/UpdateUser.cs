using System.Text.Json;
using CommunityToolkit.Diagnostics;
using Immediate.Apis.Shared;
using Immediate.Handlers.Shared;
using LinqToDB;
using Microsoft.AspNetCore.Authorization;
using VsaTemplate.Api.Database;
using VsaTemplate.Api.Features.Users.Models;
using VsaTemplate.Api.Infrastructure.Authorization;

namespace VsaTemplate.Api.Features.Users.Endpoints;

[Handler]
[MapPut("/api/users/active")]
[Authorize(Policy = Policies.Admin)]
public static partial class UpdateUser
{
	public sealed record Query
	{
		public required UserId UserId { get; set; }
		public required string EmailAddress { get; init; }
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
			.Where(u => u.UserId == query.UserId.Value)
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
