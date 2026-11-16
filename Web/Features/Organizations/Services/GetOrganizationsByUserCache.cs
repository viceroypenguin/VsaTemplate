using System.Globalization;
using Immediate.Cache.Shared;
using VsaTemplate.Web.Features.Organizations.Queries;

namespace VsaTemplate.Web.Features.Organizations.Services;

[CacheFor<GetOrganizationsByUser>]
public sealed partial class GetOrganizationsByUserCache
{
	protected override string TransformKey(GetOrganizationsByUser.Query request) =>
		string.Create(CultureInfo.InvariantCulture, $"GetOrganizationsByUserCache-{request.UserId}");
}
