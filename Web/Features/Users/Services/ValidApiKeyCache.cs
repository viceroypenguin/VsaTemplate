using System.Globalization;
using Immediate.Handlers.Shared;
using Microsoft.Extensions.Caching.Memory;
using VsaTemplate.Web.Features.Users.Queries;
using VsaTemplate.Web.Infrastructure.Caching;
using VsaTemplate.Web.Infrastructure.DependencyInjection;

namespace VsaTemplate.Web.Features.Users.Services;

[RegisterSingleton]
public sealed class ValidApiKeyCache(
	IMemoryCache memoryCache,
	Owned<IHandler<GetApiKey.Request, GetApiKey.Response>> ownedIsValidApiKey
) : ApplicationCacheBase<GetApiKey.Request, GetApiKey.Response>(
	memoryCache,
	ownedIsValidApiKey
)
{
	protected override string TransformKey(GetApiKey.Request request) =>
		string.Create(CultureInfo.InvariantCulture, $"Valid-ApiKey-{request.ApiKey}");
}
