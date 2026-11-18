using System.Text.Json;
using Immediate.Apis.Shared;
using Immediate.Handlers.Shared;
using Immediate.Validations.Shared;
using LinqToDB;
using Microsoft.AspNetCore.Mvc;
using VsaTemplate.Web.Database;
using VsaTemplate.Web.Features.AccessControl.Authorization;
using VsaTemplate.Web.Features.AccessControl.Services;
using VsaTemplate.Web.Features.Organizations.Models;
using VsaTemplate.Web.Features.Tenants.Authorization;
using VsaTemplate.Web.Features.Tenants.Models;

namespace VsaTemplate.Web.Features.Organizations.Endpoints;

[Handler]
[MapPut("/api/tenants/{tenantId}/organizations")]
public sealed partial class CreateOrganization(
	CurrentUserService currentUserService,
	DbContext context,
	PermissionCacheInvalidator permissionCacheInvalidator
)
{
	[Validate]
	public sealed partial record Request : IAuthorizedRequest, ITenantRequest, IValidationTarget<Request>
	{
		public static TenantPermission TenantPermission => TenantPermission.OrganizationManagement;

		[FromRoute]
		public required TenantId TenantId { get; init; }

		[FromBody]
		public required RequestBody Body { get; init; }
	}

	[Validate]
	public sealed partial record RequestBody : IValidationTarget<RequestBody>
	{
		[NotEmpty]
		[MaxLength(200)]
		public required string Name { get; init; }
	}

	public sealed record Response
	{
		public required OrganizationId OrganizationId { get; init; }
	}

	private async ValueTask<Response> HandleAsync([AsParameters] Request request, CancellationToken token)
	{
		var userId = currentUserService.GetLoggedInUserId();
		var memberUserId = currentUserService.GetMembershipUserId();
		await using var transaction = await context.BeginTransactionAsync(token);

		var organizationId = (OrganizationId)await context.InsertWithInt32IdentityAsync(
			new Database.Models.Organization
			{
				TenantId = request.TenantId,
				Name = request.Body.Name.Trim(),
				IsActive = true,
			}, token: token);

		var roleId = (OrganizationRoleId)await context.InsertWithInt32IdentityAsync(
			new Database.Models.OrganizationRole
			{
				OrganizationId = organizationId,
				Name = "Admin",
				PermissionsJson = JsonSerializer.Serialize(new[] { OrganizationPermission.Admin }),
				EditedUserId = userId,
			}, token: token);

		await context.InsertAsync(new Database.Models.OrganizationRoleUser
		{
			OrganizationRoleId = roleId,
			UserId = memberUserId,
			EditedUserId = userId,
		}, token: token);

		await transaction.CommitAsync(token);
		await permissionCacheInvalidator.InvalidateOrganization(organizationId);
		return new() { OrganizationId = organizationId };
	}
}
