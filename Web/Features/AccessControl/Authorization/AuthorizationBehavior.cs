using Immediate.Handlers.Shared;
using VsaTemplate.Web.Features.AccessControl.Models;
using VsaTemplate.Web.Features.AccessControl.Services;

namespace VsaTemplate.Web.Features.AccessControl.Authorization;

public sealed class AuthorizationBehavior<TRequest, TResponse>(
	CurrentUserService currentUserService
) : Behavior<TRequest, TResponse>
	where TRequest : IAuthorizedRequest
{
	public override async ValueTask<TResponse> HandleAsync(TRequest request, CancellationToken cancellationToken)
	{
		var permission = TRequest.Permission;

		if (permission is not Permission.None
			&& !await currentUserService.GetCurrentUserPermissions().HasPermission(permission))
		{
			throw new UnauthorizedAccessException();
		}

		return await Next(request, cancellationToken);
	}
}
