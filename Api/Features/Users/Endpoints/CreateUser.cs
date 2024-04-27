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
[MapPost("/api/users/create")]
[Authorize(Policy = Policies.Admin)]
public static partial class CreateUser
{
	public sealed record Command
	{
		public required string EmailAddress { get; init; }
		public required string Name { get; init; }
		public required bool IsActive { get; init; }
		public required IReadOnlyList<string> Roles { get; init; }
	}

	private static async ValueTask<User> HandleAsync(
		Command query,
		DbContext context,
		CancellationToken token)
	{
		Guard.IsNotNullOrWhiteSpace(query.Name);
		Guard.IsNotNullOrWhiteSpace(query.EmailAddress);

		var user = await context.Users
			.InsertWithOutputAsync(
				new Database.Models.User
				{
					Name = query.Name,
					EmailAddress = query.EmailAddress,
					IsActive = query.IsActive,
					Roles = JsonSerializer.Serialize(query.Roles),
				},
				token: token);

		return user.ToDto();
	}
}
