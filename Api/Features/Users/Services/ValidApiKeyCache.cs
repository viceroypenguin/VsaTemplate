using System.Globalization;
using Immediate.Handlers.Shared;
using Microsoft.Extensions.Caching.Memory;
using VsaTemplate.Api.Features.Users.Queries;
using VsaTemplate.Api.Infrastructure.Caching;
using VsaTemplate.Api.Infrastructure.DependencyInjection;

namespace VsaTemplate.Api.Features.Users.Services;

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
