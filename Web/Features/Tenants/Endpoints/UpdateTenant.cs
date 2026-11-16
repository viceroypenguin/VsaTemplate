using Immediate.Apis.Shared;
using Immediate.Handlers.Shared;
using LinqToDB;
using Microsoft.AspNetCore.Mvc;
using VsaTemplate.Web.Database;
using VsaTemplate.Web.Features.AccessControl.Authorization;
using VsaTemplate.Web.Features.AccessControl.Services;
using VsaTemplate.Web.Features.Shared.Exceptions;
using VsaTemplate.Web.Features.Tenants.Models;
using VsaTemplate.Web.Features.Tenants.Services;

namespace VsaTemplate.Web.Features.Tenants.Endpoints;

[Handler]
[MapPut("/api/tenants/{tenantId}")]
public sealed partial class UpdateTenant(
	DbContext context,
	CurrentUserService currentUserService,
	TenantAdministrationCache administration,
	PermissionCacheInvalidator cacheInvalidator
)
{
	public sealed record Request : IAuthorizedRequest
	{
		[FromRoute] public required TenantId TenantId { get; init; }
		[FromBody] public required RequestBody Body { get; init; }
	}

	public sealed record RequestBody
	{
		public required string Name { get; init; }
		public required bool IsActive { get; init; }
	}

	private async ValueTask HandleAsync([AsParameters] Request request, CancellationToken token)
	{
		var permissions = await administration.GetValue(
			new()
			{
				UserId = currentUserService.GetLoggedInUserId(),
				TenantId = request.TenantId,
			},
			token
		);

		if (!permissions.Permissions.Contains(TenantPermission.Admin))
			UnauthorizedException.ThrowUnauthorizedException();

		var name = TenantManagement.ValidateName(request.Body.Name);

		var count = await context.Tenants
			.Where(t => t.TenantId == request.TenantId)
			.Set(t => t.Name, name)
			.Set(t => t.IsActive, request.Body.IsActive)
			.UpdateAsync(token);

		if (count == 0)
			throw new NotFoundException(nameof(Database.Models.Tenant));

		await cacheInvalidator.InvalidateTenant(request.TenantId, CancellationToken.None);
	}
}
