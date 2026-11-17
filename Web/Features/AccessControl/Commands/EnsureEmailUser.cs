using Immediate.Handlers.Shared;
using VsaTemplate.Web.Database;
using VsaTemplate.Web.Features.AccessControl.Exceptions;
using VsaTemplate.Web.Features.AccessControl.Models;
using VsaTemplate.Web.Features.AccessControl.Services;
using VsaTemplate.Web.Features.AccessControl.Validators;

namespace VsaTemplate.Web.Features.AccessControl.Commands;

[Handler]
public sealed partial class EnsureEmailUser(
	EmailLoginService emailLoginService,
	DbContext context
)
{
	public sealed record Command
	{
		public required string EmailAddress { get; init; }
	}

	private async ValueTask<UserId> HandleAsync(Command command, CancellationToken token)
	{
		var email = EmailLoginValidation.NormalizeEmail(command.EmailAddress);

		await using var transaction = await context.BeginTransactionAsync(token);

		var user = await emailLoginService.EnsureAsync(email, loginAt: null, token)
			?? throw new EmailLoginException();

		await transaction.CommitAsync(token);

		return user.UserId;
	}
}
