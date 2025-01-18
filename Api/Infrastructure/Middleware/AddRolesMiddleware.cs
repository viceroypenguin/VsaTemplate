using VsaTemplate.Api.Features.Users.Services;

namespace VsaTemplate.Api.Infrastructure.Middleware;

[RegisterSingleton(typeof(AddRolesMiddleware))]
public sealed class AddRolesMiddleware(
	CurrentUserService userService
) : IMiddleware
{
	public async Task InvokeAsync(HttpContext context, RequestDelegate next)
	{
		if (context.User is { } user)
		{
			user.AddIdentity(
				await userService.GetRoleClaimsIdentity(user)
			);
		}

		await next(context);
	}
}
