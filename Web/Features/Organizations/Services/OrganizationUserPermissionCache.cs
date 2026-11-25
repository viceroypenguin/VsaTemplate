using System.Globalization;
using Immediate.Cache.Shared;
using Immediate.Handlers.Shared;
using VsaTemplate.Web.Features.AccessControl.Models;
using VsaTemplate.Web.Features.Organizations.Models;
using VsaTemplate.Web.Features.Organizations.Queries;

namespace VsaTemplate.Web.Features.Organizations.Services;

[CacheFor<OrganizationGetUserPermissions>]
public sealed partial class OrganizationUserPermissionCache
{
	protected override string TransformKey(OrganizationGetUserPermissions.Query query) =>
		string.Create(
			CultureInfo.InvariantCulture,
			$"OrganizationUserPermissionCache-{query.UserId}-{query.OrganizationId}"
		);
}

[Handler]
public sealed partial class OrganizationGetUserPermissions(GetOrganizationsForUser.Handler getOrganizationsForUser)
{
	private async ValueTask<Response> HandleAsync(Query query, CancellationToken token)
	{
		var organizations = await getOrganizationsForUser.HandleAsync(
			new() { UserId = query.UserId, OrganizationId = query.OrganizationId },
			token
		);

		return new() { Permissions = organizations.SingleOrDefault()?.Permissions ?? [] };
	}

	public sealed record Query
	{
		public required UserId UserId { get; init; }

		public required OrganizationId OrganizationId { get; init; }
	}

	public sealed record Response
	{
		public required IReadOnlyList<OrganizationPermission> Permissions { get; init; }
	}
}
