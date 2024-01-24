using System.Text.Json;
using Riok.Mapperly.Abstractions;

namespace VsaTemplate.Web.Features.Users.Models;

[Mapper]
internal static partial class Mapper
{
	internal static partial IQueryable<User> SelectDto(this IQueryable<Database.Models.User> q);

	private static UserId ToUserId(int id) => (UserId)id;
	private static Auth0UserId ToAuth0UserId(string id) => (Auth0UserId)id;
	private static IReadOnlyList<string> ToRoles(string roles) => JsonSerializer.Deserialize<List<string>>(roles)!;

	[MapperIgnoreSource(nameof(Database.Models.User.Features))]
	internal static partial User ToDto(this Database.Models.User user);
}
