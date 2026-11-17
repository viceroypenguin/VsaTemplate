using Immediate.Handlers.Shared;
using VsaTemplate.Web.Features.AccessControl.Models;
using VsaTemplate.Web.Features.Organizations.Models;
using VsaTemplate.Web.Features.Organizations.Services;

namespace VsaTemplate.Web.Features.Organizations.Queries;

[Handler]
public sealed partial class OrganizationGetUserPermissions(OrganizationPermissionReader permissionReader)
{
	public sealed record Query
	{
		public required UserId UserId { get; init; }

		public required OrganizationId OrganizationId { get; init; }
	}

	public sealed record Response
	{
		public required IReadOnlyList<OrganizationPermission> Permissions { get; init; }
	}

	private async ValueTask<Response> HandleAsync(Query query, CancellationToken token) => new()
	{
		Permissions = await permissionReader.Read(query.UserId, query.OrganizationId, includeInactive: false, token),
	};
}
