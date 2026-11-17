using System.Net.Mail;
using System.Diagnostics.CodeAnalysis;
using VsaTemplate.Web.Features.AccessControl.Exceptions;

namespace VsaTemplate.Web.Features.AccessControl.Validators;

internal static class EmailLoginValidation
{
	[SuppressMessage("Globalization", "CA1308:Normalize strings to uppercase", Justification = "Canonical email addresses use lowercase for storage and display.")]
	public static string NormalizeEmail(string? emailAddress)
	{
		var email = emailAddress?.Trim();

		if (string.IsNullOrEmpty(email)
			|| email.Length > 500
			|| !MailAddress.TryCreate(email, out var address)
			|| !string.Equals(address.Address, email, StringComparison.OrdinalIgnoreCase))
		{
			throw new EmailLoginException();
		}

		return email.ToLowerInvariant();
	}

	public static string NormalizeSession(string? sessionId)
	{
		if (!Guid.TryParseExact(sessionId, "D", out var session) || session == Guid.Empty)
			throw new EmailLoginException();

		return session.ToString("D");
	}
}
