using System.Text.Json;
using Immediate.Apis.Shared;
using Immediate.Handlers.Shared;
using Immediate.Validations.Shared;
using LinqToDB;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;
using VsaTemplate.Api.Database;
using VsaTemplate.Api.Features.Users.Models;
using VsaTemplate.Api.Infrastructure.Authorization;

namespace VsaTemplate.Api.Features.Users.Endpoints;

[Handler]
[MapPost("/api/users/create")]
[Authorize(Policy = Policies.Admin)]
public static partial class CreateUser
{
	internal static Created<Response> TransformResult(Response response) =>
		TypedResults.Created($"/api/users/{response.UserId}", response);

	[Validate]
	public sealed partial record Command : IValidationTarget<Command>
	{
		public static string? Policy => Policies.Admin;

		[NotEmpty]
		public required string EmailAddress { get; init; }

		[NotEmpty]
		public required string Name { get; init; }

		public required bool IsActive { get; init; }
		public required IReadOnlyList<string> Roles { get; init; }
	}

	public sealed record Response
	{
		public required UserId UserId { get; init; }
	}

	private static async ValueTask<Response> HandleAsync(
		Command query,
		DbContext context,
		CancellationToken token
	)
	{
		var userId = await context
			.InsertWithInt32IdentityAsync(
				new Database.Models.User
				{
					Name = query.Name,
					EmailAddress = query.EmailAddress,
					IsActive = query.IsActive,
					Roles = JsonSerializer.Serialize(query.Roles),
				},
				token: token
			);

		return new()
		{
			UserId = UserId.From(userId),
		};
	}
}
