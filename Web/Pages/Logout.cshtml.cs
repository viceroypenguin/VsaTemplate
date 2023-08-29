using Auth0.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace VsaTemplate.Web.Pages;

public class LogoutModel : PageModel
{
	public async Task OnGet()
	{
		if (HttpContext.User.Identity == null)
			Response.Redirect("/");

		var authenticationProperties = new LogoutAuthenticationPropertiesBuilder()
			 .WithRedirectUri("/")
			 .Build();

		await HttpContext.SignOutAsync(Auth0Constants.AuthenticationScheme, authenticationProperties);
		await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
	}
}
