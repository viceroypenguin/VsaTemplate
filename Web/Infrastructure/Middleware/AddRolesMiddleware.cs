using VsaTemplate.Web.Features.Users.Services;

namespace VsaTemplate.Web.Infrastructure.Middleware;

[RegisterSingleton(typeof(AddRolesMiddleware))]
public sealed class AddRolesMiddleware(
	CurrentUserService userService
) : IMiddleware
{
	public async Task InvokeAsync(HttpContext context, RequestDelegate next)
	{
		if (context.User is { Identity.IsAuthenticated: true } user)
		{
			user.AddIdentity(
				await userService.GetRoleClaimsIdentity(user)
			);
		}

		await next(context);
	}
}
