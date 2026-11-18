using System.Globalization;
using Immediate.Cache.Shared;
using Immediate.Handlers.Shared;
using VsaTemplate.Web.Features.AccessControl.Models;
using VsaTemplate.Web.Features.Organizations.Models;

namespace VsaTemplate.Web.Features.Organizations.Services;

[CacheFor<GetOrganizationAdministrationPermissions>]
public sealed partial class OrganizationAdministrationCache
{
	protected override string TransformKey(GetOrganizationAdministrationPermissions.Query query) =>
		string.Create(CultureInfo.InvariantCulture, $"OrganizationAdministrationCache-{query.UserId}-{query.OrganizationId}");
}

[Handler]
public sealed partial class GetOrganizationAdministrationPermissions(OrganizationPermissionReader permissionReader)
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

	private async ValueTask<Response> HandleAsync(Query query, CancellationToken token)
	{
		var permissions = await permissionReader.Read(
			query.UserId,
			query.OrganizationId,
			includeInactive: true,
			token
		);

		return new()
		{
			Permissions = permissions.Contains(OrganizationPermission.Admin) ? [OrganizationPermission.Admin] : [],
		};
	}
}
