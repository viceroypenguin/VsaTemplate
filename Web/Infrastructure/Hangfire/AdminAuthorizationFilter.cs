using System.Security.Claims;
using Hangfire.Dashboard;
using VsaTemplate.Web.Infrastructure.Authorization;

namespace VsaTemplate.Web.Infrastructure.Hangfire;

internal sealed class AdminAuthorizationFilter : IDashboardAuthorizationFilter
{
	public bool Authorize(DashboardContext context) =>
		context.GetHttpContext().User
			.HasClaim(ClaimTypes.Role, Policies.Admin);
}
