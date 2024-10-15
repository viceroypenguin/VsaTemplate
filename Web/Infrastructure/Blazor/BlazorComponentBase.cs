using System.Reflection;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.AspNetCore.Components.Server;

namespace VsaTemplate.Web.Infrastructure.Blazor;

public partial class BlazorComponentBase : OwningComponentBase
{
	[Inject] private NavigationManager NavigationManager { get; set; } = default!;
	[Inject] private ILogger<BlazorComponentBase> Logger { get; set; } = default!;
	[Inject] private AuthenticationStateProvider AuthenticationStateProvider { get; set; } = default!;

	protected override void OnInitialized()
	{
		NavigationManager.LocationChanged += NavigationManager_LocationChanged;

		if (ScopedServices.GetRequiredService<AuthenticationStateProvider>() is ServerAuthenticationStateProvider stateProvider)
			stateProvider.SetAuthenticationState(AuthenticationStateProvider.GetAuthenticationStateAsync());

		var properties = GetType()
			.GetProperties(
				BindingFlags.DeclaredOnly
				| BindingFlags.Instance
				| BindingFlags.NonPublic
				| BindingFlags.Public
			)
			.Where(p => p.GetCustomAttribute<InjectScopedAttribute>() is not null);

		foreach (var p in properties)
			p.SetValue(this, ScopedServices.GetService(p.PropertyType));
	}

	private void NavigationManager_LocationChanged(object? sender, LocationChangedEventArgs e) =>
		Logger.LogInformation($"Page Navigation to {e.Location.Named("Url")}");

	protected override void Dispose(bool disposing)
	{
		NavigationManager.LocationChanged -= NavigationManager_LocationChanged;
		base.Dispose(disposing);
	}
}
