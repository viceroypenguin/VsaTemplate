using System.Globalization;
using Immediate.Cache.Shared;
using VsaTemplate.Web.Features.AccessControl.Queries;

namespace VsaTemplate.Web.Features.AccessControl.Services;

[CacheFor<GetUserPermissions>]
public sealed partial class UserPermissionCache
{
	protected override string TransformKey(GetUserPermissions.Query request) =>
		string.Create(CultureInfo.InvariantCulture, $"UserPermissionCache-{request.UserId}");
}
