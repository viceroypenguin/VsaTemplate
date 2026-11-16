using Immediate.Apis.Shared;
using Immediate.Handlers.Shared;
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
[MapPut("/api/tenants/{tenantId}")]
public sealed partial class UpdateTenant(DbContext context, PermissionCacheInvalidator cacheInvalidator)
{
	public sealed record Request : IAuthorizedRequest, ITenantRequest
	{
		public static TenantPermission TenantPermission => TenantPermission.Admin;
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
		var name = TenantManagement.ValidateName(request.Body.Name);
		var count = await context.Tenants.Where(t => t.TenantId == request.TenantId)
			.Set(t => t.Name, name)
			.Set(t => t.IsActive, request.Body.IsActive)
			.UpdateAsync(token);
		if (count == 0)
			throw new NotFoundException(nameof(Database.Models.Tenant));
		await cacheInvalidator.InvalidateTenant(request.TenantId, CancellationToken.None);
	}
}
