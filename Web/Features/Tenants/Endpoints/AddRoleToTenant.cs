using System.Text.Json;
using Immediate.Apis.Shared;
using Immediate.Handlers.Shared;
using LinqToDB;
using Microsoft.AspNetCore.Mvc;
using VsaTemplate.Web.Database;
using VsaTemplate.Web.Features.AccessControl.Authorization;
using VsaTemplate.Web.Features.AccessControl.Services;
using VsaTemplate.Web.Features.Tenants.Authorization;
using VsaTemplate.Web.Features.Tenants.Models;
using VsaTemplate.Web.Features.Tenants.Services;

namespace VsaTemplate.Web.Features.Tenants.Endpoints;

[Handler]
[MapPut("/api/tenants/{tenantId}/roles")]
public sealed partial class AddRoleToTenant(
	CurrentUserService currentUserService,
	CurrentTenantUserService currentTenantUserService,
	DbContext context
)
{
	public sealed record Request : IAuthorizedRequest, ITenantRequest
	{
		public static TenantPermission TenantPermission => TenantPermission.UserManagement;
		[FromRoute] public required TenantId TenantId { get; init; }
		[FromBody] public required RequestBody RequestBody { get; init; }
	}

	public sealed record RequestBody
	{
		public required string Name { get; init; }
		public required IReadOnlyList<TenantPermission> Permissions { get; init; }
	}

	public sealed record Response
	{
		public required TenantRoleId RoleId { get; init; }
	}

	private async ValueTask<Response> HandleAsync([AsParameters] Request request, CancellationToken token)
	{
		var name = TenantManagement.ValidateName(request.RequestBody.Name);
		var permissions = TenantManagement.ValidatePermissions(request.RequestBody.Permissions);
		TenantManagement.EnsureCanGrant(
			await currentTenantUserService.GetLoggedInUserPermissions(request.TenantId), permissions);
		var userId = await currentUserService.GetLoggedInUserId();

		var roleId = TenantRoleId.From(await context.InsertWithInt32IdentityAsync(
			new Database.Models.TenantRole
			{
				TenantId = request.TenantId,
				Name = name,
				PermissionsJson = JsonSerializer.Serialize(permissions),
				EditedUserId = userId,
			}, token: token));
		return new() { RoleId = roleId };
	}
}
