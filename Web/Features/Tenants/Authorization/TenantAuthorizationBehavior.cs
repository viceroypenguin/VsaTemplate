using Immediate.Handlers.Shared;
using VsaTemplate.Web.Features.AccessControl.Models;
using VsaTemplate.Web.Features.AccessControl.Services;
using VsaTemplate.Web.Features.Shared.Exceptions;
using VsaTemplate.Web.Features.Tenants.Models;
using VsaTemplate.Web.Features.Tenants.Services;

namespace VsaTemplate.Web.Features.Tenants.Authorization;

public sealed partial class TenantAuthorizationBehavior<TRequest, TResponse>(
	CurrentUserService currentUserService,
	CurrentTenantUserService currentTenantUserService,
	ILogger<TenantAuthorizationBehavior<TRequest, TResponse>> logger
) : Behavior<TRequest, TResponse>
	where TRequest : ITenantRequest
{
	public override async ValueTask<TResponse> HandleAsync(TRequest request, CancellationToken cancellationToken)
	{
		var tenantId = request.TenantId;
		var permission = TRequest.TenantPermission;

		if (
			await currentTenantUserService.GetCurrentUserPermissions(tenantId) is { } permissions
			&& permissions.HasPermission(permission)
		)
		{
			return await Next(request, cancellationToken);
		}

		var userId = await currentUserService.GetCurrentUserId();

		LogUnauthorizedAccess(logger, userId, tenantId, HandlerType.FullName, permission);
		return UnauthorizedException.ThrowUnauthorizedException<TResponse>();
	}

	[LoggerMessage(
		Level = LogLevel.Warning,
		Message = "Unauthorized tenant access: User {UserId}, TenantId {TenantId}, Handler {HandlerType}, Permission {Permission}"
	)]
	private static partial void LogUnauthorizedAccess(ILogger logger, UserId? userId, TenantId tenantId, string? handlerType, TenantPermission permission);
}
