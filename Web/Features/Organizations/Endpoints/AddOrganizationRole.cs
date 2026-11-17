using System.Text.Json;
using Immediate.Apis.Shared;
using Immediate.Handlers.Shared;
using Immediate.Validations.Shared;
using LinqToDB;
using Microsoft.AspNetCore.Mvc;
using VsaTemplate.Web.Database;
using VsaTemplate.Web.Features.AccessControl.Authorization;
using VsaTemplate.Web.Features.AccessControl.Services;
using VsaTemplate.Web.Features.Organizations.Authorization;
using VsaTemplate.Web.Features.Organizations.Models;
using VsaTemplate.Web.Features.Organizations.Services;

namespace VsaTemplate.Web.Features.Organizations.Endpoints;

[Handler]
[MapPost("/api/organizations/{organizationId}/roles/create")]
public sealed partial class AddOrganizationRole(
	CurrentUserService currentUserService,
	CurrentOrganizationUserService currentOrganizationUserService,
	DbContext context,
	PermissionCacheInvalidator permissionCacheInvalidator
)
{
	[Validate]
	public sealed partial record Request : IAuthorizedRequest, IOrganizationRequest, IValidationTarget<Request>
	{
		public static OrganizationPermission OrganizationPermission => OrganizationPermission.UserManagement;

		[FromRoute]
		public required OrganizationId OrganizationId { get; init; }

		[FromBody]
		public required RequestBody Body { get; init; }
	}

	[Validate]
	public sealed partial record RequestBody : IValidationTarget<RequestBody>
	{
		[NotEmpty]
		[MaxLength(200)]
		public required string Name { get; init; }

		[NotEmpty]
		public required IReadOnlyList<OrganizationPermission> Permissions { get; init; }
	}

	public sealed record Response
	{
		public required OrganizationRoleId RoleId { get; init; }
	}

	private async ValueTask<Response> HandleAsync([AsParameters] Request request, CancellationToken token)
	{
		var name = request.Body.Name.Trim();

		var permissions = request.Body.Permissions
			.Distinct()
			.ToArray();

		await OrganizationManagement.DemandDelegablePermissions(currentOrganizationUserService, request.OrganizationId, permissions);

		var roleId = (OrganizationRoleId)await context.InsertWithInt32IdentityAsync(
			new Database.Models.OrganizationRole
			{
				OrganizationId = request.OrganizationId,
				Name = name,
				PermissionsJson = JsonSerializer.Serialize(permissions),
				EditedUserId = currentUserService.GetLoggedInUserId(),
			}, token: token);

		await permissionCacheInvalidator.InvalidateOrganization(request.OrganizationId, CancellationToken.None);
		return new() { RoleId = roleId };
	}
}
