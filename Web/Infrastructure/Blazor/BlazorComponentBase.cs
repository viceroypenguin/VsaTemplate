using System.Reflection;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;

namespace VsaTemplate.Web.Infrastructure.Blazor;

public partial class BlazorComponentBase : OwningComponentBase
{
	[Inject] private NavigationManager NavigationManager { get; set; } = default!;
	[Inject] private ILogger<BlazorComponentBase> Logger { get; set; } = default!;

	protected override void OnInitialized()
	{
		NavigationManager.LocationChanged += NavigationManager_LocationChanged;

		var properties = GetType().GetProperties()
			.Where(p => p.GetCustomAttribute<InjectScopedAttribute>() != null);

		foreach (var p in properties)
			p.SetValue(this, ScopedServices.GetService(p.PropertyType));
	}

	private void NavigationManager_LocationChanged(object? sender, LocationChangedEventArgs e) =>
		LogLocationChanged(Logger, e.Location);

	protected override void Dispose(bool disposing)
	{
		NavigationManager.LocationChanged -= NavigationManager_LocationChanged;
		base.Dispose(disposing);
	}

	[LoggerMessage(LogLevel.Information, "Page Navigation to {Url}")]
	private static partial void LogLocationChanged(ILogger logger, string url);
}
