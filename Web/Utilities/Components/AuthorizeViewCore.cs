using System.Diagnostics;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace VsaTemplate.Web.Utilities.Components;

/// <summary>
/// A base class for components that display differing content depending on the user's authorization status.
/// </summary>
public abstract class AuthorizeViewCore : ComponentBase
{
	private enum AuthorizationState { Authorizing, NotAuthorized, Authorized }
	private AuthorizationState _state;

	/// <summary>
	/// The content that will be displayed if the user is authorized.
	/// </summary>
	[Parameter] public RenderFragment? ChildContent { get; set; }

	/// <summary>
	/// The content that will be displayed if the user is not authorized.
	/// </summary>
	[Parameter] public RenderFragment? NotAuthorized { get; set; }

	/// <summary>
	/// The content that will be displayed if the user is authorized.
	/// If you specify a value for this parameter, do not also specify a value for <see cref="ChildContent"/>.
	/// </summary>
	[Parameter] public RenderFragment? Authorized { get; set; }

	/// <summary>
	/// The content that will be displayed while asynchronous authorization is in progress.
	/// </summary>
	[Parameter] public RenderFragment? Authorizing { get; set; }

	/// <inheritdoc />
	protected override void BuildRenderTree(RenderTreeBuilder builder)
	{
		// We're using the same sequence number for each of the content items here
		// so that we can update existing instances if they are the same shape
		var content = _state switch
		{
			AuthorizationState.Authorizing => Authorizing,
			AuthorizationState.NotAuthorized => NotAuthorized,
			AuthorizationState.Authorized => Authorized ?? ChildContent,
			_ => throw new UnreachableException(),
		};

		builder.AddContent(0, content);
	}

	/// <inheritdoc />
	protected override async Task OnParametersSetAsync()
	{
		// We allow 'ChildContent' for convenience in basic cases, and 'Authorized' for symmetry
		// with 'NotAuthorized' in other cases. Besides naming, they are equivalent. To avoid
		// confusion, explicitly prevent the case where both are supplied.
		if (ChildContent is not null && Authorized is not null)
			throw new InvalidOperationException($"Do not specify both '{nameof(Authorized)}' and '{nameof(ChildContent)}'.");

		// Clear the previous result of authorization
		// This will cause the Authorizing state to be displayed until the authorization has been completed
		_state = AuthorizationState.Authorizing;

		StateHasChanged();

		_state = await IsAuthorizedAsync() ? AuthorizationState.Authorized : AuthorizationState.NotAuthorized;
	}

	/// <summary>
	/// Determines whether the current user is authorized to view the contents of this component.
	/// </summary>
	/// <returns>
	/// <see langword="true" /> if the user is authorized to view this component;
	/// <see langword="false" /> otherwise.
	/// </returns>
	protected abstract ValueTask<bool> IsAuthorizedAsync();
}
