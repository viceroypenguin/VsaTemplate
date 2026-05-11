using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Immediate.Handlers.Shared;
using VsaTemplate.Web.Features.AccessControl.Models;
using VsaTemplate.Web.Features.AccessControl.Services;
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

		if (permission is not TenantPermission.None
			&& !await currentTenantUserService.GetCurrentUserPermissions(tenantId).HasPermission(permission))
		{
			var userId = await currentUserService.GetCurrentUserId();

			LogUnauthorizedAccess(logger, userId, tenantId, HandlerType.FullName, permission);
			ThrowUnauthorizedAccess();
		}

		return await Next(request, cancellationToken);
	}

	[LoggerMessage(
		Level = LogLevel.Warning,
		Message = "Unauthorized tenant access: User {UserId}, TenantId {TenantId}, Handler {HandlerType}, Permission {Permission}"
	)]
	private static partial void LogUnauthorizedAccess(ILogger logger, UserId? userId, TenantId tenantId, string? handlerType, TenantPermission permission);

	[StackTraceHidden]
	[DoesNotReturn]
	private static void ThrowUnauthorizedAccess() =>
		throw new UnauthorizedAccessException();
}
