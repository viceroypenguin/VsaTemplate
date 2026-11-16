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
using VsaTemplate.Web.Features.Shared.Exceptions;

namespace VsaTemplate.Web.Features.Organizations.Endpoints;

[Handler]
[MapPut("/api/organizations/{organizationId}")]
public sealed partial class UpdateOrganization(DbContext context, PermissionCacheInvalidator permissionCacheInvalidator)
{
	[Validate]
	public sealed partial record Request : IAuthorizedRequest, IOrganizationRequest, IValidationTarget<Request>
	{
		public static OrganizationPermission OrganizationPermission => OrganizationPermission.Admin;

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
		public required bool IsActive { get; init; }
	}

	private async ValueTask HandleAsync([AsParameters] Request request, CancellationToken token)
	{
		var count = await context.Organizations
			.Where(o => o.OrganizationId == request.OrganizationId)
			.Set(o => o.Name, OrganizationManagement.ValidateName(request.Body.Name))
			.Set(o => o.IsActive, request.Body.IsActive)
			.UpdateAsync(token);
		if (count == 0)
			NotFoundException.ThrowNotFoundException(nameof(Database.Models.Organization));
		await permissionCacheInvalidator.InvalidateOrganization(request.OrganizationId, CancellationToken.None);
	}
}
