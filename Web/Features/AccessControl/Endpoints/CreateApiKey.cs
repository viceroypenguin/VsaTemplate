using System.Security.Cryptography;
using System.Text.Json;
using Immediate.Apis.Shared;
using Immediate.Handlers.Shared;
using Immediate.Validations.Shared;
using LinqToDB;
using SimpleBase;
using VsaTemplate.Web.Database;
using VsaTemplate.Web.Features.AccessControl.Authorization;
using VsaTemplate.Web.Features.AccessControl.Models;
using VsaTemplate.Web.Features.AccessControl.Services;

namespace VsaTemplate.Web.Features.Users.Endpoints;

[Handler]
[MapPost("/api/users/apikey/create")]
public sealed partial class CreateApiKey(
	CurrentUserService currentUserService,
	DbContext context
)
{
	[Validate]
	public sealed partial record Request : IAuthorizedRequest, IValidationTarget<Request>
	{
		public required IReadOnlyList<Permission> Permissions { get; init; }

		[LessThan(nameof(s_maxLifetime), Message = "Api Key cannot be have a lifetime longer than one year.")]
		public required TimeSpan Lifetime { get; init; }

		private static readonly TimeSpan s_maxLifetime = TimeSpan.FromDays(366);
	}

	public sealed record Response
	{
		public required string ApiKey { get; init; }
	}

	private async ValueTask<Response> HandleAsync(
		Request request,
		CancellationToken token
	)
	{
		var userId = await currentUserService.GetLoggedInUserId();
		var currentUserPermissions = await currentUserService.GetLoggedInUserPermissions();

		if (request.Permissions.Except(currentUserPermissions).Any())
			throw new InvalidOperationException("Unable to create new API Key with additional permission.");

		var key = GenerateApiKey();
		var now = DateTimeOffset.UtcNow;

		await using var transaction = await context.BeginTransactionAsync(token);

		var newUserId = UserId.From(
			await context.InsertWithInt32IdentityAsync(
				new Database.Models.AccessControl.User()
				{
					Name = string.Create(provider: null, $"API Key For: {userId}"),
					EmailAddress = key,
					IsActive = true,
				},
				token: token
			)
		);

		await context.InsertAsync(
			new Database.Models.AccessControl.ApiKey()
			{
				ApiKeyId = newUserId,
				OwnerUserId = userId,
				CreatedDateTime = now,
				ExpirationDateTime = now.Add(request.Lifetime),
				PermissionsJson = JsonSerializer.Serialize(currentUserPermissions),
			},
			token: token
		);

		await transaction.CommitAsync(token);

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
