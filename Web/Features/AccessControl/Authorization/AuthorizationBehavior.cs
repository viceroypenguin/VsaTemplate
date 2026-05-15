using Immediate.Handlers.Shared;
using VsaTemplate.Web.Features.AccessControl.Models;
using VsaTemplate.Web.Features.AccessControl.Services;
using VsaTemplate.Web.Features.Shared.Exceptions;

namespace VsaTemplate.Web.Features.AccessControl.Authorization;

public sealed partial class AuthorizationBehavior<TRequest, TResponse>(
	CurrentUserService currentUserService,
	ILogger<AuthorizationBehavior<TRequest, TResponse>> logger
) : Behavior<TRequest, TResponse>
	where TRequest : IAuthorizedRequest
{
	public override async ValueTask<TResponse> HandleAsync(TRequest request, CancellationToken cancellationToken)
	{
		var permissions = await currentUserService.GetLoggedInUserPermissions();
		var permission = TRequest.Permission;

		if (permission is not Permission.None
			&& !permissions.HasPermission(permission))
		{
			var userId = await currentUserService.GetCurrentUserId();

			LogUnauthorizedAccess(logger, userId, HandlerType.FullName, permission);
			UnauthorizedException.ThrowUnauthorizedException();
		}

		return await Next(request, cancellationToken);
	}

	[LoggerMessage(
		Level = LogLevel.Warning,
		Message = "Unauthorized access: User {UserId}, Handler {HandlerType}, Permission {Permission}"
	)]
	private static partial void LogUnauthorizedAccess(ILogger logger, UserId? userId, string? handlerType, Permission permission);
}
