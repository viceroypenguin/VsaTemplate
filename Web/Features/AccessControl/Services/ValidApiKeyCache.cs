using Immediate.Cache.Shared;
using VsaTemplate.Web.Features.AccessControl.Queries;

namespace VsaTemplate.Web.Features.AccessControl.Services;

[CacheFor<GetApiKey>]
public sealed partial class ValidApiKeyCache
{
	protected override string TransformKey(GetApiKey.Request request) =>
		$"Valid-ApiKey-{request.ApiKey}";
}
