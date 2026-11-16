using System.Globalization;
using Immediate.Cache.Shared;
using VsaTemplate.Web.Features.Organizations.Queries;

namespace VsaTemplate.Web.Features.Organizations.Services;

[CacheFor<GetOrganizationAdministrationPermissions>]
public sealed partial class OrganizationAdministrationCache
{
	protected override string TransformKey(GetOrganizationAdministrationPermissions.Query query) =>
		string.Create(CultureInfo.InvariantCulture, $"OrganizationAdministrationCache-{query.UserId}-{query.OrganizationId}");
}
