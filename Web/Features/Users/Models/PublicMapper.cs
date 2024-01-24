using Riok.Mapperly.Abstractions;

namespace VsaTemplate.Web.Features.Users.Models;

[Mapper(UseDeepCloning = true)]
public static partial class PublicMapper
{
	public static partial User Clone(this User user);

	public static partial void CloneTo(this User user, User target);
}
