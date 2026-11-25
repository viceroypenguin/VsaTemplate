using System.Data;
using System.Text.Json;
using Immediate.Apis.Shared;
using Immediate.Handlers.Shared;
using Immediate.Validations.Shared;
using LinqToDB;
using LinqToDB.Async;
using Microsoft.AspNetCore.Mvc;
using VsaTemplate.Web.Database;
using VsaTemplate.Web.Features.AccessControl.Authorization;
using VsaTemplate.Web.Features.AccessControl.Services;
using VsaTemplate.Web.Features.Organizations.Models;
using VsaTemplate.Web.Features.Shared.Exceptions;
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
	private async ValueTask<Response> HandleAsync([AsParameters] Request request, CancellationToken token)
	{
		var userId = currentUserService.GetLoggedInUserId();
		var memberUserId = currentUserService.GetMembershipUserId();
		// Keep the tenant active through the insert so deactivation can include the new organization.
		await using var transaction = await context.BeginTransactionAsync(IsolationLevel.Serializable, token);

		if (!await context.Tenants.AnyAsync(t => t.TenantId == request.TenantId && t.IsActive, token))
			throw new NotFoundException(nameof(Database.Models.Tenant));

		var organizationId = (OrganizationId)await context.InsertWithInt32IdentityAsync(
			new Database.Models.Organization
			{
				TenantId = request.TenantId,
				Name = request.Body.Name.Trim(),
				IsActive = true,
			},
			token: token
		);

		var roleId = (OrganizationRoleId)await context.InsertWithInt32IdentityAsync(
			new Database.Models.OrganizationRole
			{
				OrganizationId = organizationId,
				Name = "Admin",
				PermissionsJson = JsonSerializer.Serialize(new[] { OrganizationPermission.Admin }),
				EditedUserId = userId,
			},
			token: token
		);

		await context.InsertAsync(
			new Database.Models.OrganizationRoleUser
			{
				OrganizationRoleId = roleId,
				UserId = memberUserId,
				EditedUserId = userId,
			},
			token: token
		);

		await transaction.CommitAsync(token);
		await permissionCacheInvalidator.InvalidateOrganization(organizationId);
		return new() { OrganizationId = organizationId };
	}

	[Validate]
	public sealed partial record Request : IAuthorizedRequest, ITenantRequest, IValidationTarget<Request>
	{
		[FromBody]
		public required RequestBody Body { get; init; }

		public static TenantPermission TenantPermission => TenantPermission.OrganizationManagement;

		[FromRoute]
		public required TenantId TenantId { get; init; }
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
}
