using System.Linq.Expressions;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.CompilerServices;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Rendering;

namespace VsaTemplate.Web.Components;

public sealed partial class BulmaInputText<TValue, TComponent> : BlazorComponentBase
	where TComponent : InputBase<TValue>
{
	[CascadingParameter] private EditContext CascadedEditContext { get; set; } = null!;

	/// <summary>
	/// Gets or sets a collection of additional attributes that will be applied to the created element.
	/// </summary>
	[Parameter(CaptureUnmatchedValues = true)] public IReadOnlyDictionary<string, object>? AdditionalAttributes { get; set; }

	/// <summary>
	/// Gets or sets the value of the input. This should be used with two-way binding.
	/// </summary>
	/// <example>
	/// @bind-Value="model.PropertyName"
	/// </example>
	[Parameter]
	public TValue? Value { get; set; }

	/// <summary>
	/// Gets or sets a callback that updates the bound value.
	/// </summary>
	[Parameter] public EventCallback<TValue> ValueChanged { get; set; }

	/// <summary>
	/// Gets or sets an expression that identifies the bound value.
	/// </summary>
	[Parameter] public Expression<Func<TValue>>? ValueExpression { get; set; }

	private FieldIdentifier _fieldIdentifier;

	protected override void OnParametersSet()
	{
		if (CascadedEditContext is null)
			throw new InvalidOperationException("Cascaded EditContext must be present.");

		if (ValueExpression is null)
			throw new InvalidOperationException("@bind-Value must be used.");

		_fieldIdentifier = FieldIdentifier.Create(ValueExpression);
		CascadedEditContext.OnFieldChanged += OnFieldChanged;
	}

	protected override void Dispose(bool disposing)
	{
		CascadedEditContext.OnFieldChanged -= OnFieldChanged;
		base.Dispose(disposing);
	}

	private void OnFieldChanged(object? sender, FieldChangedEventArgs args)
	{
		if (!EqualityComparer<FieldIdentifier>.Default.Equals(args.FieldIdentifier, _fieldIdentifier))
			return;

		StateHasChanged();
	}

	protected override void BuildRenderTree(RenderTreeBuilder builder)
	{
		var state = GetState();
		UpdateAttributesForState(state);

		builder.OpenElement(0, "div");
		builder.AddAttribute(1, "class", "field");
		builder.OpenElement(2, "div");
		builder.AddAttribute(3, "class", "control has-icons-right");
		builder.OpenComponent<TComponent>(4);
		builder.AddComponentParameter(5, "Value", RuntimeHelpers.TypeCheck(Value));
		builder.AddComponentParameter(6, "ValueChanged", RuntimeHelpers.TypeCheck(EventCallback.Factory.Create(this, ValueChanged)));
		builder.AddComponentParameter(7, "ValueExpression", RuntimeHelpers.TypeCheck(ValueExpression));
		builder.AddComponentParameter(8, "AdditionalAttributes", RuntimeHelpers.TypeCheck(AdditionalAttributes));
		builder.CloseComponent();

		if (state is not State.None)
		{
			builder.OpenElement(9, "span");
			builder.AddAttribute(10, "class", "icon is-small is-right");
			builder.OpenElement(12, "i");
			builder.AddAttribute(12, "class", "fas " + (state is State.Invalid ? "fa-exclamation-triangle" : "fa-check"));
			builder.CloseElement();
			builder.CloseElement();
		}

		builder.CloseElement();

		if (state is State.Invalid)
		{
			builder.AddMarkupContent(13, "<p class=\"help is-danger\">Organization Name must be provided.</p>");
		}

		builder.CloseElement();
	}

	private void UpdateAttributesForState(State state) =>
		AdditionalAttributes = new Dictionary<string, object>(
			AdditionalAttributes?.AsEnumerable() ?? [],
			StringComparer.OrdinalIgnoreCase
		)
		{
			["class"] = GetInputCssForState(state),
		};

	private State GetState()
	{
		if (!CascadedEditContext.IsValid(_fieldIdentifier))
			return State.Invalid;

		if (!CascadedEditContext.IsModified(_fieldIdentifier))
			return State.None;

		return State.Valid;
	}

	private static string GetInputCssForState(State state) =>
		state switch
		{
			State.Valid => "input is-success",
			State.Invalid => "input is-danger",
			State.None or _ => "input",
		};

	private enum State
	{
		None = 0,
		Valid = 1,
		Invalid = 2,
	}
}
