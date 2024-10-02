using System.Text.Json;
using Immediate.Apis.Shared;
using Immediate.Handlers.Shared;
using Immediate.Validations.Shared;
using LinqToDB;
using Microsoft.AspNetCore.Authorization;
using VsaTemplate.Api.Database;
using VsaTemplate.Api.Features.Shared.Exceptions;
using VsaTemplate.Api.Features.Users.Models;
using VsaTemplate.Api.Infrastructure.Authorization;

namespace VsaTemplate.Api.Features.Users.Endpoints;

[Handler]
[MapPut("/api/users/active")]
[Authorize(Policy = Policies.Admin)]
public static partial class UpdateUser
{
	[Validate]
	public sealed partial record Query : IValidationTarget<Query>
	{
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
			NotFoundException.ThrowNotFoundException("User");

		return rows[0].ToDto();
	}
}
