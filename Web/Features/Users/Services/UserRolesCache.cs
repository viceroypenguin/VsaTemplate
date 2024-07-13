using System.Globalization;
using Immediate.Handlers.Shared;
using Microsoft.Extensions.Caching.Memory;
using VsaTemplate.Web.Features.Users.Queries;
using VsaTemplate.Web.Infrastructure.Caching;
using VsaTemplate.Web.Infrastructure.DependencyInjection;

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
