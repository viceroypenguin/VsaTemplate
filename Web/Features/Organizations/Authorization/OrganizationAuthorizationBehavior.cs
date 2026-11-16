using Immediate.Handlers.Shared;
using LinqToDB.Async;
using VsaTemplate.Web.Database;
using VsaTemplate.Web.Features.AccessControl.Models;
using VsaTemplate.Web.Features.AccessControl.Services;
using VsaTemplate.Web.Features.Shared.Exceptions;
using VsaTemplate.Web.Features.Organizations.Models;
using VsaTemplate.Web.Features.Organizations.Services;
using VsaTemplate.Web.Features.Tenants.Models;
using VsaTemplate.Web.Features.Tenants.Services;

namespace VsaTemplate.Web.Features.Organizations.Authorization;

public sealed partial class OrganizationAuthorizationBehavior<TRequest, TResponse>(
	CurrentUserService currentUserService,
	CurrentOrganizationUserService currentOrganizationUserService,
	CurrentTenantUserService currentTenantUserService,
	DbContext context,
	ILogger<OrganizationAuthorizationBehavior<TRequest, TResponse>> logger
) : Behavior<TRequest, TResponse>
	where TRequest : IOrganizationRequest
{
	public override async ValueTask<TResponse> HandleAsync(TRequest request, CancellationToken cancellationToken)
	{
		var organizationId = request.OrganizationId;
		var permission = TRequest.OrganizationPermission;
		var organization = await context.Organizations
			.Where(o => o.OrganizationId == organizationId)
			.Select(o => new { o.TenantId, o.IsActive, TenantIsActive = o.Tenant.IsActive })
			.FirstOrDefaultAsync(cancellationToken);

		if (
			organization is { TenantIsActive: true }
			&& (organization.IsActive || permission == OrganizationPermission.Admin)
			&& await currentTenantUserService.GetCurrentUserPermissions(organization.TenantId)
				.HasPermission(TenantPermission.None)
			&& await currentOrganizationUserService.GetCurrentUserPermissions(organizationId) is { } permissions
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
