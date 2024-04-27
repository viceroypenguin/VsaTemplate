using System.Security.Claims;
using Hangfire.Dashboard;
using VsaTemplate.Api.Infrastructure.Authorization;

namespace VsaTemplate.Api.Infrastructure.Hangfire;

public sealed class AdminAuthorizationFilter : IDashboardAuthorizationFilter
{
	public bool Authorize(DashboardContext context) =>
		context.GetHttpContext().User
			.HasClaim(ClaimTypes.Role, Policies.Admin);
}
