using Immediate.Apis.Shared;
using Immediate.Handlers.Shared;
using LinqToDB;
using LinqToDB.Async;
using Microsoft.AspNetCore.Mvc;
using VsaTemplate.Web.Database;
using VsaTemplate.Web.Features.AccessControl.Authorization;
using VsaTemplate.Web.Features.AccessControl.Models;
using VsaTemplate.Web.Features.AccessControl.Services;
using VsaTemplate.Web.Features.Shared.Exceptions;

namespace VsaTemplate.Web.Features.AccessControl.Endpoints;

[Handler]
[MapDelete("/api/users/apikey/{apiKeyId}")]
public sealed partial class InvalidateApiKey(
	CurrentUserService currentUserService,
	DbContext context,
	ValidApiKeyCache validApiKeyCache,
	PermissionCacheInvalidator permissionCacheInvalidator
)
{
	private async ValueTask HandleAsync([AsParameters] Request request, CancellationToken token)
	{
		var ownerId = currentUserService.GetLoggedInHumanUserId();
		await using var transaction = await context.BeginTransactionAsync(token);

		var ownedKey = context.AccessControl.ApiKeys
			.Where(k => k.ApiKeyId == request.ApiKeyId && k.OwnerUserId == ownerId);

		var secret = await ownedKey
			.Select(k => k.User.EmailAddress)
			.SingleOrDefaultAsync(token);

		if (secret is null)
			NotFoundException.ThrowNotFoundException("API key");

		await context.TenantApiKeys
			.Where(k => k.TenantApiKeyId == request.ApiKeyId)
			.DeleteAsync(token);

		await context.OrganizationApiKeys
			.Where(k => k.OrganizationApiKeyId == request.ApiKeyId)
			.DeleteAsync(token);

		if (await ownedKey.DeleteAsync(token) == 0)
			NotFoundException.ThrowNotFoundException("API key");

		// Preserve audit references to the synthetic user and erase its credential.
		await context.AccessControl.Users
			.Where(u => u.UserId == request.ApiKeyId)
			.Set(u => u.IsActive, value: false)
			.Set(u => u.EmailAddress, string.Create(provider: null, $"Revoked API key {request.ApiKeyId}"))
			.UpdateAsync(token);

		await transaction.CommitAsync(CancellationToken.None);

		validApiKeyCache.RemoveValue(new() { ApiKey = secret });
		await permissionCacheInvalidator.InvalidateUser(request.ApiKeyId);
	}

	public sealed record Request : IAuthorizedRequest
	{
		[FromRoute]
		public required UserId ApiKeyId { get; init; }
	}
}
