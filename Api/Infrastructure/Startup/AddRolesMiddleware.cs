using System.Security.Claims;
using VsaTemplate.Api.Features.Users.Models;
using VsaTemplate.Api.Features.Users.Services;
using VsaTemplate.Api.Infrastructure.Authorization;

namespace VsaTemplate.Api.Infrastructure.Startup;

[RegisterSingleton(typeof(AddRolesMiddleware))]
public sealed class AddRolesMiddleware(
	UserRolesCache userRolesCache
) : IMiddleware
{
	public async Task InvokeAsync(HttpContext context, RequestDelegate next)
	{
		if (context.User is { } user)
		{
			var claim = user.FindFirstValue(Claims.Id) ?? "";
			if (UserId.TryParse(claim, out var userId))
			{
				var roles = await userRolesCache.GetValue(new() { UserId = userId, });

				user.AddIdentity(
					new ClaimsIdentity(
						roles
							.Select(r => new Claim(ClaimTypes.Role, r))
					)
				);
			}
		}

		await next(context);
	}
}
