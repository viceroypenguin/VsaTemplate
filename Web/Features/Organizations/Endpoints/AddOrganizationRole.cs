using System.Text.Json;
using Immediate.Apis.Shared;
using Immediate.Handlers.Shared;
using LinqToDB;
using Microsoft.AspNetCore.Mvc;
using VsaTemplate.Web.Database;
using VsaTemplate.Web.Features.AccessControl.Authorization;
using VsaTemplate.Web.Features.AccessControl.Services;
using VsaTemplate.Web.Features.Organizations.Authorization;
using VsaTemplate.Web.Features.Organizations.Models;

namespace VsaTemplate.Web.Features.Organizations.Endpoints;

[Handler]
[MapPost("/api/organizations/{organizationId}/roles/create")]
public sealed partial class AddOrganizationRole(CurrentUserService currentUserService, DbContext context)
{
	public sealed record Request : IAuthorizedRequest, IOrganizationRequest
	{
		public static OrganizationPermission Demand => OrganizationPermission.UserManagement;

		[FromRoute]
		public required OrganizationId OrganizationId { get; init; }

		[FromBody]
		public required RequestBody Body { get; init; }
	}

	public readonly record struct RequestBody
	{
		public required string Name { get; init; }
		public required IReadOnlyList<OrganizationPermission> Permissions { get; init; }
	}

	public sealed record Response
	{
		public required OrganizationRoleId OrganizationRoleId { get; init; }
	}

	private async ValueTask<Response> HandleAsync(
		[AsParameters] Request request,
		CancellationToken token
	)
	{
		var userId = await currentUserService.GetCurrentUserId();

		var organizationRoleId = (OrganizationRoleId)await context
			.InsertWithInt32IdentityAsync(
				new Database.Models.VsaTemplate.OrganizationRole
				{
					OrganizationId = request.OrganizationId,
					Name = request.Body.Name,
					PermissionsJson = JsonSerializer.Serialize(request.Body.Permissions),
					EditedUserId = userId,
				},
				token: token
			);

		return new()
		{
			OrganizationRoleId = organizationRoleId,
		};
	}
}
