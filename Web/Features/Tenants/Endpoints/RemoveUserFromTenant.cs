using Immediate.Apis.Shared;
using Immediate.Handlers.Shared;
using LinqToDB;
using VsaTemplate.Web.Database;
using VsaTemplate.Web.Features.AccessControl.Authorization;
using VsaTemplate.Web.Features.AccessControl.Models;
using VsaTemplate.Web.Features.AccessControl.Services;
using VsaTemplate.Web.Features.Shared.Exceptions;
using VsaTemplate.Web.Features.Tenants.Authorization;
using VsaTemplate.Web.Features.Tenants.Models;

namespace VsaTemplate.Web.Features.VsaTemplate.Endpoints;

[Handler]
[MapDelete("/api/tenants/{tenantId}/users/{userId}")]
public sealed partial class RemoveUserFromTenant(
	CurrentUserService currentUserService,
	DbContext context
)
{
	public sealed record Request : IAuthorizedRequest, ITenantRequest
	{
		public TenantPermission TenantPermission => TenantPermission.UserManagement;

		public required TenantId TenantId { get; init; }
		public required UserId UserId { get; init; }
	}

	private async ValueTask HandleAsync(
		[AsParameters] Request request,
		CancellationToken token
	)
	{
		if (await context.AccountUsers.Where(au => au.AccountId == request.AccountId).CountAsync(token) <= 1)
		{
			throw new ConflictException(
				$"Unable to remove the self from account when no other administrators are available."
			);
		}

		var userId = await currentUserService.GetCurrentUserId();

		await using var transaction = await context.BeginTransactionAsync(token);

		await context.AccountUsers
			.Where(au => au.AccountId == request.AccountId)
			.Where(au => au.UserId == request.UserId)
			.UpdateAsync(
				au => new() { EditedUserId = userId },
				token: token
			);

		await context.AccountUsers
			.Where(au => au.AccountId == request.AccountId)
			.Where(au => au.UserId == request.UserId)
			.DeleteAsync(
				token: token
			);

		await transaction.CommitAsync(token);
	}
}
