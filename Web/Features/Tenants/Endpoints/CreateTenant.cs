using System.Security.Cryptography;
using System.Text.Json;
using Immediate.Apis.Shared;
using Immediate.Handlers.Shared;
using Immediate.Validations.Shared;
using LinqToDB;
using SimpleBase;
using VsaTemplate.Web.Database;
using VsaTemplate.Web.Features.AccessControl.Authorization;
using VsaTemplate.Web.Features.AccessControl.Services;
using VsaTemplate.Web.Features.Shared.Validators;
using VsaTemplate.Web.Features.Tenants.Models;

namespace VsaTemplate.Web.Features.Tenants.Endpoints;

[Handler]
[MapPost("/api/tenants/create")]
public sealed partial class CreateTenant(
	CurrentUserService currentUserService,
	DbContext context,
	PermissionCacheInvalidator cacheInvalidator
)
{
	private async ValueTask<Response> HandleAsync(
		Request request,
		CancellationToken token
	)
	{
		var userId = currentUserService.GetLoggedInUserId();
		var ownerId = currentUserService.GetMembershipUserId();
		var name = request.Name.Trim();
		var recoveryKeys = BuildRecoveryKeys();

		await using var transaction = await context.BeginTransactionAsync(token);

		var tenantId = (TenantId)await context
			.InsertWithInt32IdentityAsync(
				new Database.Models.Tenant
				{
					Name = name,
					IsActive = true,
					RecoveryKeysJson = JsonSerializer.Serialize(recoveryKeys),
				},
				token: token
			);

		var tenantRoleId = (TenantRoleId)await context
			.InsertWithInt32IdentityAsync(
				new Database.Models.TenantRole
				{
					TenantId = tenantId,
					Name = "Owner",
					PermissionsJson = "[1]",
					EditedUserId = userId,
				},
				token: token
			);

		await context.InsertAsync(
			new Database.Models.TenantRoleUser
			{
				UserId = ownerId,
				TenantRoleId = tenantRoleId,
				EditedUserId = userId,
			},
			token: token
		);

		await transaction.CommitAsync(token);
		await cacheInvalidator.InvalidateUser(ownerId);

		return new()
		{
			TenantId = tenantId,
			RecoveryKeys = recoveryKeys,
		};
	}

	private static List<string> BuildRecoveryKeys()
	{
		return Enumerable.Range(0, 12)
			.Select(
				_ =>
					Base58.Bitcoin.Encode(RandomNumberGenerator.GetBytes(16))
			)
			.ToList();
	}

	[Validate]
	public sealed partial record Request : IAuthorizedRequest, IValidationTarget<Request>
	{
		[NotEmpty]
		[TrimmedMaxLength(200)]
		public required string Name { get; init; }
	}

	public sealed record Response
	{
		public required TenantId TenantId { get; init; }

		public required IReadOnlyList<string> RecoveryKeys { get; init; }
	}
}
