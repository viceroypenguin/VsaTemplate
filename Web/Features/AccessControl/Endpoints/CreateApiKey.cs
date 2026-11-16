using System.Security.Cryptography;
using System.Text.Json;
using Immediate.Apis.Shared;
using Immediate.Handlers.Shared;
using Immediate.Validations.Shared;
using LinqToDB;
using LinqToDB.Async;
using SimpleBase;
using VsaTemplate.Web.Database;
using VsaTemplate.Web.Features.AccessControl.Authorization;
using VsaTemplate.Web.Features.AccessControl.Exceptions;
using VsaTemplate.Web.Features.AccessControl.Models;
using VsaTemplate.Web.Features.AccessControl.Services;
using VsaTemplate.Web.Features.Shared.Exceptions;

namespace VsaTemplate.Web.Features.AccessControl.Endpoints;

[Handler]
[MapPost("/api/users/apikey/create")]
public sealed partial class CreateApiKey(
	CurrentUserService currentUserService,
	DbContext context,
	TimeProvider timeProvider
)
{
	[Validate]
	public sealed partial record Request : IAuthorizedRequest, IValidationTarget<Request>
	{
		public required IReadOnlyList<Permission> Permissions { get; init; }

		[LessThan(nameof(MaxLifetime), Message = "Api Key cannot be have a lifetime longer than one year.")]
		public required TimeSpan Lifetime { get; init; }

		private static readonly TimeSpan MaxLifetime = TimeSpan.FromDays(366);
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

		if (await context.AccessControl.ApiKeys
			.AnyAsync(k => k.ApiKeyId == userId, token))
			throw new UnauthorizedException("API keys cannot create other API keys.");

		if (request.Lifetime <= TimeSpan.Zero || request.Lifetime >= TimeSpan.FromDays(366))
			throw new InvalidApiKeyInputException("API key lifetime must be positive and shorter than one year.");

		if (request.Permissions is null || request.Permissions
			.Any(p => !Enum.IsDefined(p)))
			throw new InvalidApiKeyInputException("Select valid API key permissions.");

		if (!currentUserPermissions.Contains(Permission.Admin)
			&& request.Permissions
				.Any(p => p is not Permission.None && !currentUserPermissions.Contains(p)))
			throw new UnauthorizedException("Unable to create an API key with additional permissions.");

		var key = GenerateApiKey();
		var now = timeProvider.GetUtcNow();

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
				PermissionsJson = JsonSerializer.Serialize(request.Permissions
					.Distinct()
					.ToArray()),
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
