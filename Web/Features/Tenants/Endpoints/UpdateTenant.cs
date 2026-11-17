using Immediate.Apis.Shared;
using Immediate.Handlers.Shared;
using Immediate.Validations.Shared;
using LinqToDB;
using Microsoft.AspNetCore.Mvc;
using VsaTemplate.Web.Database;
using VsaTemplate.Web.Features.AccessControl.Authorization;
using VsaTemplate.Web.Features.AccessControl.Services;
using VsaTemplate.Web.Features.Shared.Exceptions;
using VsaTemplate.Web.Features.Shared.Validators;
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
	[Validate]
	public sealed partial record Request : IAuthorizedRequest, IValidationTarget<Request>
	{
		[FromRoute]
		public required TenantId TenantId { get; init; }

		[FromBody]
		public required RequestBody Body { get; init; }
	}

	[Validate]
	public sealed partial record RequestBody : IValidationTarget<RequestBody>
	{
		[NotEmpty]
		[TrimmedMaxLength(200)]
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

		var name = request.Body.Name.Trim();

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
