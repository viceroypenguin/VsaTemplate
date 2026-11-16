using System.Globalization;
using Immediate.Cache.Shared;
using VsaTemplate.Web.Features.Organizations.Queries;

namespace VsaTemplate.Web.Features.Organizations.Services;

[CacheFor<OrganizationGetUserPermissions>]
public sealed partial class OrganizationUserPermissionCache
{
	protected override string TransformKey(OrganizationGetUserPermissions.Query query) =>
		string.Create(CultureInfo.InvariantCulture, $"OrganizationUserPermissionCache-{query.UserId}-{query.OrganizationId}");
}
