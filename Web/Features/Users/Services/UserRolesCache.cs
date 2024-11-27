using System.Globalization;
using Immediate.Cache;
using Immediate.Handlers.Shared;
using Microsoft.Extensions.Caching.Memory;
using VsaTemplate.Web.Features.Users.Queries;

namespace VsaTemplate.Web.Features.Users.Services;

[RegisterSingleton]
public sealed class UserRolesCache(
	IMemoryCache memoryCache,
	Owned<IHandler<GetUserRoles.Query, IReadOnlyList<string>>> ownedGetUserRoles
) : ApplicationCacheBase<GetUserRoles.Query, IReadOnlyList<string>>(
	memoryCache,
	ownedGetUserRoles
)
{
	protected override string TransformKey(GetUserRoles.Query request) =>
		string.Create(CultureInfo.InvariantCulture, $"User-Roles-{request.UserId}");
}
