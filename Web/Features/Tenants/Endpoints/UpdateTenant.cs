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
using VsaTemplate.Web.Features.Tenants.Authorization;
using VsaTemplate.Web.Features.Tenants.Models;

namespace VsaTemplate.Web.Features.Tenants.Endpoints;

[Handler]
[MapPut("/api/tenants/{tenantId}")]
public sealed partial class UpdateTenant(
	DbContext context,
	PermissionCacheInvalidator cacheInvalidator
)
{
	private async ValueTask HandleAsync([AsParameters] Request request, CancellationToken token)
	{
		var name = request.Body.Name.Trim();
		await using var transaction = await context.BeginTransactionAsync(token);

		var count = await context.Tenants
			.Where(t => t.TenantId == request.TenantId && t.IsActive)
			.Set(t => t.Name, name)
			.Set(t => t.IsActive, request.Body.IsActive)
			.UpdateAsync(token);

		if (count == 0)
			throw new NotFoundException(nameof(Database.Models.Tenant));

		if (!request.Body.IsActive)
		{
			await context.Organizations
				.Where(o => o.TenantId == request.TenantId && o.IsActive)
				.Set(o => o.IsActive, value: false)
				.UpdateAsync(token);
		}

		await transaction.CommitAsync(token);
		await cacheInvalidator.InvalidateTenant(request.TenantId);
	}

	[Validate]
	public sealed partial record Request : IAuthorizedRequest, ITenantRequest, IValidationTarget<Request>
	{
		public static TenantPermission TenantPermission => TenantPermission.Admin;

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
}
