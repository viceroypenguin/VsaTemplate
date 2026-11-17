using Immediate.Handlers.Shared;
using Immediate.Validations.Shared;
using VsaTemplate.Web.Database;
using VsaTemplate.Web.Features.AccessControl.Exceptions;
using VsaTemplate.Web.Features.AccessControl.Models;
using VsaTemplate.Web.Features.AccessControl.Services;
using VsaTemplate.Web.Features.Shared.Validators;

namespace VsaTemplate.Web.Features.AccessControl.Commands;

[Handler]
public sealed partial class EnsureEmailUser(
	EmailLoginService emailLoginService,
	DbContext context
)
{
	[Validate]
	public sealed partial record Command : IValidationTarget<Command>
	{
		[NotEmpty]
		[TrimmedMaxLength(500)]
		[BareEmailAddress]
		public required string EmailAddress { get; init; }
	}

	private async ValueTask<UserId> HandleAsync(Command command, CancellationToken token)
	{
		var email = EmailLoginNormalization.NormalizeEmail(command.EmailAddress);

		await using var transaction = await context.BeginTransactionAsync(token);

		var user = await emailLoginService.EnsureAsync(email, loginAt: null, token)
			?? throw new EmailLoginException();

		await transaction.CommitAsync(token);

		return user.UserId;
	}
}
