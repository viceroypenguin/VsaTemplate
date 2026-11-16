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
using VsaTemplate.Web.Features.Organizations.Services;
using VsaTemplate.Web.Features.Tenants.Authorization;
using VsaTemplate.Web.Features.Tenants.Models;

namespace VsaTemplate.Web.Features.VsaTemplate.Endpoints;

[Handler]
[MapPut("api/tenants/{tenantId}/organizations")]
public sealed partial class CreateOrganization(
	CurrentUserService currentUserService,
	DbContext context,
	GetOrganizationsByUserCache getOrganizationsByUserCache
)
{
	[Validate]
	public sealed partial record Request : IAuthorizedRequest, ITenantRequest, IValidationTarget<Request>
	{
		public TenantPermission TenantPermission => TenantPermission.OrganizationManagement;

		[FromRoute]
		public required TenantId TenantId { get; init; }

		[FromBody]
		public required RequestBody RequestBody { get; init; }
	}

	[Validate]
	public sealed partial record RequestBody : IValidationTarget<RequestBody>
	{
		[NotEmpty]
		public required string Name { get; init; }
	}

	public sealed record Response
	{
		public required OrganizationId OrganizationId { get; init; }
	}

	private async ValueTask<Response> HandleAsync(
		Request request,
		CancellationToken token
	)
	{
		var userId = await currentUserService.GetCurrentUserId();

		var organizationId = (OrganizationId)await context
			.InsertWithInt32IdentityAsync(
				new Database.Models.VsaTemplate.Organization
				{
					AccountId = request.AccountId,
					Name = request.Name,
					IsActive = true,
					EditedUserId = userId,
				},
				token: token
			);

		var organizationRoleId = (OrganizationRoleId)await context
			.InsertWithInt32IdentityAsync(
				new Database.Models.VsaTemplate.OrganizationRole
				{
					OrganizationId = organizationId,
					Name = "Admin",
					PermissionsJson = JsonSerializer.Serialize(new[] { OrganizationPermission.Admin }),
					EditedUserId = userId,
				},
				token: token
			);

		await context
			.InsertAsync(
				new Database.Models.VsaTemplate.OrganizationRoleUser
				{
					OrganizationRoleId = organizationRoleId,
					UserId = userId,
					EditedUserId = userId,
				},
				token: token
			);

		await getOrganizationsByUserCache.InvalidateForSelf();

		return new()
		{
			OrganizationId = organizationId,
		};
	}
}
