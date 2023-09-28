using System.Security.Claims;
using Hangfire.Dashboard;

namespace VsaTemplate.Web.Code;

public class RolesBasedAuthorizationFilter : IDashboardAuthorizationFilter
{
	private readonly IReadOnlyList<string> _roles;

	public RolesBasedAuthorizationFilter(IReadOnlyList<string> roles)
	{
		_roles = roles;
	}

	public bool Authorize(DashboardContext context)
	{
		var httpContext = context.GetHttpContext();

		if (httpContext.User.Identity is not ClaimsIdentity identity
			|| !identity.IsAuthenticated)
		{
			return false;
		}

		return identity.Claims
			.Where(c => string.Equals(c.Type, identity.RoleClaimType, StringComparison.Ordinal))
			.Select(c => c.Value)
			.Intersect(_roles)
			.Any();
	}
}
