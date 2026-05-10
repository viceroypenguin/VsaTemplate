using Hangfire.Dashboard;
using VsaTemplate.Web.Features.AccessControl.Services;

namespace VsaTemplate.Web.Infrastructure.Hangfire;

public sealed class AdminAuthorizationFilter : IDashboardAsyncAuthorizationFilter
{
	public async Task<bool> AuthorizeAsync(DashboardContext context)
	{
		var currentUserService = context.GetHttpContext().RequestServices
			.GetRequiredService<CurrentUserService>();
		return await currentUserService.GetCurrentUserPermissions().IsAdmin();
	}
}
