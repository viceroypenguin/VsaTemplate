using Immediate.Apis.Shared;
using Immediate.Handlers.Shared;
using LinqToDB;
using Microsoft.AspNetCore.Mvc;
using VsaTemplate.Web.Database;
using VsaTemplate.Web.Features.AccessControl.Authorization;
using VsaTemplate.Web.Features.AccessControl.Models;
using VsaTemplate.Web.Features.AccessControl.Services;
using VsaTemplate.Web.Features.Tenants.Authorization;
using VsaTemplate.Web.Features.Tenants.Models;

namespace VsaTemplate.Web.Features.Tenants.Endpoints;

[Handler]
[MapPut("/api/tenants/{tenantId}/roles/{roleId}/users/{userId}")]
public sealed partial class AddUserToTenant(
	CurrentUserService currentUserService,
	DbContext context
)
{
	public sealed record Request : IAuthorizedRequest, ITenantRequest
	{
		public TenantPermission TenantPermission => TenantPermission.UserManagement;

		[FromRoute]
		public required TenantId TenantId { get; init; }

		[FromRoute]
		public required TenantRoleId RoleId { get; init; }

		[FromRoute]
		public required UserId UserId { get; init; }
	}

	private async ValueTask HandleAsync(
		[AsParameters] Request request,
		CancellationToken token
	)
	{
		var userId = await currentUserService.GetLoggedInUserId();

		await context.TenantRoleUsers
			.Merge().Using([
				new Database.Models.TenantRoleUser
				{
					TenantRoleId = request.RoleId,
					UserId = request.UserId,
					EditedUserId = userId,
				},
			])
			.OnTargetKey()
			.InsertWhenNotMatched()
			.MergeAsync(token);
	}
}
