using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;
using VsaTemplate.Web.Features.Tenants.Models;

namespace VsaTemplate.Web.Features.Shared.Layout;

public sealed partial class MainLayout : LayoutComponentBase
{
	public TenantId? TenantId { get; private set; }

	protected override void OnInitialized()
	{
		NavigationManager.LocationChanged += HandleLocationChanged;
		HandleLocationChanged(new(NavigationManager.Uri));
	}

	private void HandleLocationChanged(object? sender, LocationChangedEventArgs e) => HandleLocationChanged(new(e.Location));

	private void HandleLocationChanged(Uri location)
	{
		var oldTenantId = TenantId;

		TenantId =
			TenantRegex.Match(location.AbsolutePath) is { Success: true } match
			&& Tenants.Models.TenantId.TryParse(match.Groups["tenant_id"].ValueSpan, provider: null, out var tenantId)
			? tenantId
			: null;

		if (oldTenantId != TenantId)
			StateHasChanged();
	}

	public void Dispose() => NavigationManager.LocationChanged -= HandleLocationChanged;

	[GeneratedRegex(@"^/tenant/(?<tenant_id>\d+)(/|$)", RegexOptions.ExplicitCapture)]
	private partial Regex TenantRegex { get; }
}
