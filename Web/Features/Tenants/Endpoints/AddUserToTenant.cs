using System.Data;
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
using VsaTemplate.Web.Features.Tenants.Authorization;
using VsaTemplate.Web.Features.Tenants.Models;
using VsaTemplate.Web.Features.Tenants.Services;

namespace VsaTemplate.Web.Features.Tenants.Endpoints;

[Handler]
[MapPut("/api/tenants/{tenantId}/roles/{roleId}/users/{userId}")]
public sealed partial class AddUserToTenant(
	CurrentUserService currentUserService,
	CurrentTenantUserService currentTenantUserService,
	DbContext context,
	PermissionCacheInvalidator cacheInvalidator
)
{
	private async ValueTask HandleAsync([AsParameters] Request request, CancellationToken token)
	{
		await using var transaction = await context.BeginTransactionAsync(IsolationLevel.Serializable, token);

		var role = await context.TenantRoles
				.FirstOrDefaultAsync(r => r.TenantId == request.TenantId && r.TenantRoleId == request.RoleId, token)
			?? throw new NotFoundException(nameof(Database.Models.TenantRole));

		TenantManagement.EnsureCanGrant(
			await currentTenantUserService.GetLoggedInUserPermissions(request.TenantId),
			TenantManagement.ReadPermissions(role.PermissionsJson)
		);

		if (!await context.AccessControl.Users.AnyAsync(
				u => u.UserId == request.UserId && u.IsActive && u.ApiKey == null,
				token
			))
			throw new NotFoundException(nameof(Database.Models.AccessControl.User));

		var userId = currentUserService.GetLoggedInUserId();

		await context.TenantRoleUsers
			.Merge()
			.Using(
				[
					new Database.Models.TenantRoleUser
					{
						TenantRoleId = request.RoleId,
						UserId = request.UserId,
						EditedUserId = userId,
					},
				]
			)
			.OnTargetKey()
			.InsertWhenNotMatched()
			.MergeAsync(token);

		await transaction.CommitAsync(token);
		await cacheInvalidator.InvalidateUser(request.UserId);
	}

	public sealed record Request : IAuthorizedRequest, ITenantRequest
	{
		[FromRoute]
		public required TenantRoleId RoleId { get; init; }

		[FromRoute]
		public required UserId UserId { get; init; }

		public static TenantPermission TenantPermission => TenantPermission.UserManagement;

		[FromRoute]
		public required TenantId TenantId { get; init; }
	}
}
