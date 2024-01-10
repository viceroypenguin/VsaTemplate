using Riok.Mapperly.Abstractions;

namespace VsaTemplate.Web.Features.Users.Models;

[Mapper]
internal static partial class Mapper
{
	internal static partial IQueryable<User> SelectDto(this IQueryable<Database.Models.User> q);

	private static UserId ToUserId(int id) => (UserId)id;
	private static Auth0UserId ToAuth0UserId(string id) => (Auth0UserId)id;

	[MapperIgnoreSource(nameof(Database.Models.User.Features))]
	internal static partial User ToDto(this Database.Models.User user);

	private static int FromUserId(UserId id) => (int)id;
	private static string FromAuth0UserId(Auth0UserId id) => (string)id;

	[MapperIgnoreTarget(nameof(Database.Models.User.Features))]
	internal static partial Database.Models.User FromDto(this User user);

	[MapperIgnoreTarget(nameof(Database.Models.User.Auth0UserId))]
	[MapperIgnoreTarget(nameof(Database.Models.User.Features))]
	[MapperIgnoreTarget(nameof(Database.Models.User.UserId))]
	internal static partial Database.Models.User FromDto(this CreateUserDto user);
}
