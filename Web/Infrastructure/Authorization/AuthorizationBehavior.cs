using Immediate.Handlers.Shared;
using VsaTemplate.Web.Features.Users.Services;

namespace VsaTemplate.Web.Infrastructure.Authorization;

public sealed class AuthorizationBehavior<TRequest, TResponse>(
	CurrentUserService currentUserService
) : Behavior<TRequest, TResponse>
	where TRequest : IAuthorizedRequest
{
	public override async ValueTask<TResponse> HandleAsync(TRequest request, CancellationToken cancellationToken)
	{
		var policy = TRequest.Policy;

		if (!string.IsNullOrWhiteSpace(policy)
			&& !await currentUserService.IsAuthorized(policy))
		{
			throw new UnauthorizedAccessException();
		}

		return await Next(request, cancellationToken);
	}
}
