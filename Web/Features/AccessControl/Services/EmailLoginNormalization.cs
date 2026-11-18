using System.Diagnostics.CodeAnalysis;

namespace VsaTemplate.Web.Features.AccessControl.Services;

internal static class EmailLoginNormalization
{
	[SuppressMessage(
		"Globalization",
		"CA1308:Normalize strings to uppercase",
		Justification = "Canonical email addresses use lowercase for storage and display."
	)]
	public static string NormalizeEmail(string emailAddress) =>
		emailAddress.Trim().ToLowerInvariant();

	public static string NormalizeSession(string sessionId) =>
		Guid.ParseExact(sessionId, "D").ToString("D");
}
