using System.Text.Json;
using Immediate.Apis.Shared;
using Immediate.Handlers.Shared;
using Immediate.Validations.Shared;
using LinqToDB;
using Microsoft.AspNetCore.Mvc;
using VsaTemplate.Web.Database;
using VsaTemplate.Web.Features.AccessControl.Authorization;
using VsaTemplate.Web.Features.AccessControl.Services;
using VsaTemplate.Web.Features.Shared.Exceptions;
using VsaTemplate.Web.Features.Tenants.Authorization;
using VsaTemplate.Web.Features.Tenants.Models;

namespace VsaTemplate.Web.Features.Tenants.Endpoints;

[Handler]
[MapPut("/api/tenants/{tenantId}/roles/{roleId}")]
public sealed partial class SaveTenantRole(
	CurrentUserService currentUserService,
	DbContext context
)
{
	[Validate]
	public sealed partial record Request : IAuthorizedRequest, ITenantRequest, IValidationTarget<Request>
	{
		public TenantPermission TenantPermission => TenantPermission.UserManagement;

		[FromRoute]
		public required TenantId TenantId { get; init; }

		[FromRoute]
		public required TenantRoleId RoleId { get; init; }

		[FromBody]
		public required RequestBody RequestBody { get; init; }
	}

	[Validate]
	public sealed partial record RequestBody : IValidationTarget<RequestBody>
	{
		[NotEmpty]
		public required string Name { get; init; }

		[NotEmpty]
		public required IReadOnlyList<TenantPermission> Permissions { get; init; }
	}

	private async ValueTask HandleAsync(
		[AsParameters] Request request,
		CancellationToken token
	)
	{
		var userId = await currentUserService.GetLoggedInUserId();

		var count = await context.TenantRoles
			.Where(tr => tr.TenantId == request.TenantId)
			.Where(tr => tr.TenantRoleId == request.RoleId)
			.UpdateAsync(
				dst => new()
				{
					Name = request.RequestBody.Name,
					PermissionsJson = JsonSerializer.Serialize(request.RequestBody.Permissions),
					EditedUserId = userId,
				},
				token: token
			);

		if (count == 0)
			NotFoundException.ThrowNotFoundException(nameof(Database.Models.TenantRole));
	}
}
