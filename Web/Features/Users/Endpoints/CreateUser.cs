using System.Text.Json;
using CommunityToolkit.Diagnostics;
using Immediate.Apis.Shared;
using Immediate.Handlers.Shared;
using LinqToDB;
using VsaTemplate.Web.Database;
using VsaTemplate.Web.Features.Users.Models;
using VsaTemplate.Web.Infrastructure.Authorization;

namespace VsaTemplate.Web.Features.Users.Endpoints;

[Handler]
[MapPost("/api/users/create")]
internal static partial class CreateUser
{
	internal sealed record Command : IAuthorizedRequest
	{
		public static string? Policy => Policies.Admin;

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
