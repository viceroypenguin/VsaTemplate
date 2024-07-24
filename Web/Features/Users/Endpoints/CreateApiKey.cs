using System.Security.Cryptography;
using System.Text.Json;
using Immediate.Apis.Shared;
using Immediate.Handlers.Shared;
using LinqToDB;
using SimpleBase;
using VsaTemplate.Web.Database;
using VsaTemplate.Web.Features.Users.Services;
using VsaTemplate.Web.Infrastructure.Authorization;

namespace VsaTemplate.Web.Features.Users.Endpoints;

[Handler]
[MapPost("/api/users/apikey/create")]
public static partial class CreateApiKey
{
	public sealed record Request : IAuthorizedRequest
	{
		public static string? Policy => Policies.ValidUser;
		public required IReadOnlyList<string> Roles { get; init; }
	}

	public sealed record Response
	{
		public required string ApiKey { get; init; }
	}

	private static async ValueTask<Response> HandleAsync(
		Request request,
		CurrentUserService currentUserService,
		UserRolesCache userRolesCache,
		DbContext context,
		CancellationToken token)
	{
		var userId = await currentUserService.GetCurrentUserId();
		var currentUserRoles = await userRolesCache.GetValue(new() { UserId = userId });

		if (request.Roles.Except(currentUserRoles, StringComparer.OrdinalIgnoreCase).Any())
			throw new InvalidOperationException("Unable to create new API Key with additional permission.");

		var key = GenerateApiKey();

		var newUserId = await context.InsertWithInt32IdentityAsync(
			new Database.Models.User()
			{
				Name = $"API Key For: {userId}",
				EmailAddress = key,
				IsActive = true,
				Roles = JsonSerializer.Serialize(request.Roles),
			},
			token: token
		);

		_ = await context.InsertAsync(
			new Database.Models.ApiKey()
			{
				ApiKeyId = newUserId,
				OwnerUserId = userId,
			},
			token: token
		);

		return new()
		{
			ApiKey = key,
		};
	}

	private static string GenerateApiKey()
	{
		var bytes = RandomNumberGenerator.GetBytes(16);
		return Base58.Bitcoin.Encode(bytes);
	}
}
