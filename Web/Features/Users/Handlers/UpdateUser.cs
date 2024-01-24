using System.Text.Json;
using CommunityToolkit.Diagnostics;
using Immediate.Handlers.Shared;
using LinqToDB;
using Microsoft.AspNetCore.Mvc;
using VsaTemplate.Database;
using VsaTemplate.Web.Features.Users.Models;
using VsaTemplate.Web.Infrastructure.Authorization;

namespace VsaTemplate.Web.Features.Users.Handlers;

[Handler]
[ApiController, Route("/api/users")]
public sealed partial class UpdateUser(UpdateUser.Handler handler) : ControllerBase
{
	[HttpPut]
	public async ValueTask<User> Endpoint([FromBody] Query query, CancellationToken token) =>
		await handler.HandleAsync(query, token);

	public record Query : IAuthorizedRequest
	{
		public static string? Policy => Policies.Admin;

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
