using Immediate.Handlers.Shared;
using VsaTemplate.Web.Features.Users.Services;
using VsaTemplate.Web.Infrastructure.Authorization;

namespace VsaTemplate.Web.Infrastructure.Behaviors;

public class AuthorizationBehavior<TRequest, TResponse>(
	CurrentUserService currentUserService
) : Behavior<TRequest, TResponse>
	where TRequest : IAuthorizedRequest
{
	public override async ValueTask<TResponse> HandleAsync(TRequest request, CancellationToken cancellationToken)
	{
		var policy = TRequest.Policy;
		var requirement = TRequest.Requirement;

		if (!string.IsNullOrWhiteSpace(policy) &&
			!await currentUserService.IsAuthorized(policy, requirement))
		{
			throw new UnauthorizedAccessException();
		}

		return await Next(request, cancellationToken);
	}
}
