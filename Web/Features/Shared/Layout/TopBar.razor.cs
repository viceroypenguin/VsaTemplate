using Immediate.Cache;
using Microsoft.AspNetCore.Components;
using VsaTemplate.Web.Features.Tenants.Models;
using VsaTemplate.Web.Features.Tenants.Queries;

namespace VsaTemplate.Web.Features.Shared.Layout;

public sealed partial class TopBar : ComponentBase
{
	[Inject]
	private Owned<GetTenantsForCurrentUser.Handler> GetTenantsForCurrentUser { get; set; } = null!;

	[CascadingParameter]
	public TenantId? TenantId { get; set; }

	private IReadOnlyList<GetTenantsForUser.Tenant>? _tenants;

	protected override async Task OnParametersSetAsync() => await LoadTenants();

	private async Task LoadTenants()
	{
		_tenants = null;
		await using var scope = GetTenantsForCurrentUser.GetScope(out var handler);
		_tenants = await handler.HandleAsync(new());
	}
}
