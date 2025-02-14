using Immediate.Validations.Shared;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;

namespace VsaTemplate.Web.Components;

public sealed class ImmediateValidationsValidator : ComponentBase, IDisposable
{
	private EditContext? _originalEditContext;
	private IValidationTarget? _validationTarget;
	private Subscriptions? _subscriptions;

	[CascadingParameter]
	private EditContext? CurrentEditContext { get; set; }

	protected override void OnInitialized()
	{
		if (CurrentEditContext is null)
			throw new InvalidOperationException($"{nameof(ImmediateValidationsValidator)} requires a cascading parameter of type {nameof(EditContext)}.");

		if (CurrentEditContext.Model is not IValidationTarget ivt)
			throw new InvalidOperationException($"Model must be an {nameof(IValidationTarget)}.");

		_originalEditContext = CurrentEditContext;
		_validationTarget = ivt;
		_subscriptions = new(CurrentEditContext, ivt);
	}

	protected override void OnParametersSet()
	{
		if (CurrentEditContext != _originalEditContext)
			throw new InvalidOperationException($"{nameof(ImmediateValidationsValidator)} does not support changing the {nameof(EditContext)} dynamically.");

		if (CurrentEditContext?.Model != _validationTarget)
			throw new InvalidOperationException($"{nameof(ImmediateValidationsValidator)} does not support changing the {nameof(EditContext)}.{nameof(EditContext.Model)} dynamically.");
	}

	public void Dispose()
	{
		_subscriptions?.Dispose();
		_subscriptions = null;
	}

	private sealed class Subscriptions : IDisposable
	{
		private readonly EditContext _editContext;
		private readonly IValidationTarget _validationTarget;
		private readonly ValidationMessageStore _messageStore;

		public Subscriptions(
			EditContext editContext,
			IValidationTarget validationTarget
		)
		{
			_editContext = editContext;
			_validationTarget = validationTarget;
			_messageStore = new(editContext);

			editContext.OnFieldChanged += OnFieldChanged;
			editContext.OnValidationRequested += OnValidationRequested;
		}

		private void OnValidationRequested(object? sender, ValidationRequestedEventArgs e)
			=> DoValidation();

		private void OnFieldChanged(object? sender, FieldChangedEventArgs e)
			=> DoValidation();

		private void DoValidation()
		{
			_messageStore.Clear();
			var results = _validationTarget.Validate();

			if (results is not null)
			{
				foreach (var error in results)
				{
					_messageStore.Add(
						_editContext.Field(error.PropertyName),
						error.ErrorMessage
					);
				}
			}

			_editContext.NotifyValidationStateChanged();
		}

		public void Dispose()
		{
			_messageStore.Clear();
			_editContext.OnFieldChanged -= OnFieldChanged;
			_editContext.OnValidationRequested -= OnValidationRequested;
			_editContext.NotifyValidationStateChanged();
		}
	}
}
