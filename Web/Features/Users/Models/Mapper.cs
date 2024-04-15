using System.Text.Json;

namespace VsaTemplate.Web.Features.Users.Models;

public static class Mapper
{
	public static IQueryable<User> SelectDto(this IQueryable<Database.Models.User> q) => q
		.Select(u => new User()
		{
			UserId = u.UserId,
			Name = u.Name,
			Auth0UserId = u.Auth0UserId,
			EmailAddress = u.EmailAddress,
			IsActive = u.IsActive,
			LastLogin = u.LastLogin,
			Roles = ToRoles(u.Roles),
		});

	private static List<string> ToRoles(string roles) =>
		JsonSerializer.Deserialize<List<string>>(roles)!;

	public static User ToDto(this Database.Models.User user) =>
		new User()
		{
			UserId = user.UserId,
			Name = user.Name,
			Auth0UserId = user.Auth0UserId,
			EmailAddress = user.EmailAddress,
			IsActive = user.IsActive,
			LastLogin = user.LastLogin,
			Roles = ToRoles(user.Roles),
		};
}
