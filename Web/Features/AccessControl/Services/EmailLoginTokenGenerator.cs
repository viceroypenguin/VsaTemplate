using System.Globalization;
using System.Security.Cryptography;
using Immediate.Injections.Shared;

namespace VsaTemplate.Web.Features.AccessControl.Services;

[RegisterSingleton<ILoginTokenGenerator>]
public sealed class EmailLoginTokenGenerator : ILoginTokenGenerator
{
	public string GenerateToken() =>
		RandomNumberGenerator.GetInt32(1_000_000).ToString("D6", CultureInfo.InvariantCulture);
}
