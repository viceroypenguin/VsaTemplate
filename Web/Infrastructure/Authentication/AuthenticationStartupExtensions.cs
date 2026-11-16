using System.Globalization;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.Cookies;
using LinqToDB.Async;
using VsaTemplate.Web.Database;
using VsaTemplate.Web.Features.AccessControl.Models;

namespace VsaTemplate.Web.Infrastructure.Authentication;

public static class AuthenticationStartupExtensions
{
	public static void AddWebAuthentication(this IServiceCollection services)
	{

		services
			.AddAuthentication(o =>
			{
				o.DefaultAuthenticateScheme = ApiKeyAuthenticationHandler.AuthenticationScheme;
				o.DefaultSignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
				o.DefaultChallengeScheme = CookieAuthenticationDefaults.AuthenticationScheme;
			})
			.AddCookie(o =>
			{
				o.LoginPath = "/";
				o.AccessDeniedPath = "/";
				o.Cookie.HttpOnly = true;
				o.Cookie.SameSite = SameSiteMode.Lax;
				o.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;

				o.Events.OnRedirectToLogin = context =>
				{

					if (context.Request.Path.StartsWithSegments("/api", StringComparison.OrdinalIgnoreCase))
						context.Response.StatusCode = StatusCodes.Status401Unauthorized;
					else
						context.Response.Redirect(context.RedirectUri);

					return Task.CompletedTask;
				};

				o.Events.OnRedirectToAccessDenied = context =>
				{

					if (context.Request.Path.StartsWithSegments("/api", StringComparison.OrdinalIgnoreCase))
						context.Response.StatusCode = StatusCodes.Status403Forbidden;
					else
						context.Response.Redirect(context.RedirectUri);

					return Task.CompletedTask;
				};

				o.Events.OnValidatePrincipal = async context =>
				{
					var value = context.Principal?.FindFirstValue("vsa-id");
					var db = context.HttpContext.RequestServices.GetRequiredService<DbContext>();

					if (UserId.TryParse(value ?? "", CultureInfo.InvariantCulture, out var userId)
						&& await db.AccessControl.Users
							.AnyAsync(u => u.UserId == userId && u.IsActive && u.ApiKey == null))
					{
						return;
					}

					context.RejectPrincipal();
				};

			})
			.AddScheme<ApiKeyAuthenticationSchemeOptions, ApiKeyAuthenticationHandler>(
				ApiKeyAuthenticationHandler.AuthenticationScheme,
				configureOptions: null
			);

	}
}
