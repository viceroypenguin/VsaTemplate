using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Immediate.Handlers.Shared;
using VsaTemplate.Web.Features.AccessControl.Models;
using VsaTemplate.Web.Features.AccessControl.Services;

namespace VsaTemplate.Web.Features.AccessControl.Authorization;

public sealed partial class AuthorizationBehavior<TRequest, TResponse>(
	CurrentUserService currentUserService,
	ILogger<AuthorizationBehavior<TRequest, TResponse>> logger
) : Behavior<TRequest, TResponse>
	where TRequest : IAuthorizedRequest
{
	public override async ValueTask<TResponse> HandleAsync(TRequest request, CancellationToken cancellationToken)
	{
		var permission = TRequest.Permission;

		if (permission is not Permission.None
			&& !await currentUserService.GetCurrentUserPermissions().HasPermission(permission))
		{
			var userId = await currentUserService.GetCurrentUserId();

			LogUnauthorizedAccess(logger, userId, HandlerType.FullName, permission);
			ThrowUnauthorizedAccess();
		}

		return await Next(request, cancellationToken);
	}

	[LoggerMessage(
		Level = LogLevel.Warning,
		Message = "Unauthorized access: User {UserId}, Handler {HandlerType}, Permission {Permission}"
	)]
	private static partial void LogUnauthorizedAccess(ILogger logger, UserId? userId, string? handlerType, Permission permission);

	[StackTraceHidden]
	[DoesNotReturn]
	private static void ThrowUnauthorizedAccess() =>
		throw new UnauthorizedAccessException();
}
