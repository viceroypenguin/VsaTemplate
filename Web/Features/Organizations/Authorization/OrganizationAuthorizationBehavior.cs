using Immediate.Handlers.Shared;
using VsaTemplate.Web.Features.AccessControl.Models;
using VsaTemplate.Web.Features.AccessControl.Services;
using VsaTemplate.Web.Features.Shared.Exceptions;
using VsaTemplate.Web.Features.Organizations.Models;
using VsaTemplate.Web.Features.Organizations.Services;

namespace VsaTemplate.Web.Features.Organizations.Authorization;

public sealed partial class OrganizationAuthorizationBehavior<TRequest, TResponse>(
	CurrentUserService currentUserService,
	CurrentOrganizationUserService currentOrganizationUserService,
	ILogger<OrganizationAuthorizationBehavior<TRequest, TResponse>> logger
) : Behavior<TRequest, TResponse>
	where TRequest : IOrganizationRequest
{
	public override async ValueTask<TResponse> HandleAsync(TRequest request, CancellationToken cancellationToken)
	{
		var organizationId = request.OrganizationId;
		var permission = TRequest.OrganizationPermission;

		if (
			await currentOrganizationUserService.GetCurrentUserPermissions(organizationId) is { } permissions
			&& permissions.HasPermission(permission)
		)
		{
			return await Next(request, cancellationToken);
		}

		var userId = await currentUserService.GetCurrentUserId();

		LogUnauthorizedAccess(logger, userId, organizationId, HandlerType.FullName, permission);
		return UnauthorizedException.ThrowUnauthorizedException<TResponse>();
	}

	[LoggerMessage(
		Level = LogLevel.Warning,
		Message = "Unauthorized organization access: User {UserId}, OrganizationId {OrganizationId}, Handler {HandlerType}, Permission {Permission}"
	)]
	private static partial void LogUnauthorizedAccess(ILogger logger, UserId? userId, OrganizationId organizationId, string? handlerType, OrganizationPermission permission);
}
