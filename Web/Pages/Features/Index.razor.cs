using CommunityToolkit.Diagnostics;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Radzen.Blazor;
using VsaTemplate.Features.Models;
using VsaTemplate.Features.Services;
using VsaTemplate.Users.Models;
using VsaTemplate.Users.Services;
using VsaTemplate.Web.Code;

namespace VsaTemplate.Web.Pages.Features;

public partial class Index : BlazorComponentBase
{
	[InjectScoped]
	public required FeaturesService FeaturesService { get; set; }

	[InjectScoped]
	public required UsersService UsersService { get; set; }

	[CascadingParameter]
	public required Task<AuthenticationState> AuthenticationState { get; set; }

	private readonly IReadOnlyList<FeatureType> _featureTypes =
		Enum.GetValues<FeatureType>().Where(t => t != FeatureType.None).ToList();

	private RadzenDataGrid<Feature> _featuresGrid = default!;
	private List<Feature>? _features;

	private Feature? _newFeature;
	private Feature? _editFeature;
	private Feature? _editClone;

	private UserId _currentUser;
	private string? _currentUserName;

	private bool IsLoading => _features == null;
	private bool IsEditing => _newFeature != null || _editFeature != null;

	protected override async Task OnInitializedAsync()
	{
		var authState = await AuthenticationState;
		var user = authState.User;
		var claim = user.Claims.FirstOrDefault(c => c.Type == "dbid");
		Guard.IsNotNull(claim);
		Guard.IsNotNullOrWhiteSpace(claim.Value);

		_currentUser = UserId.From(int.Parse(claim.Value, provider: null));
		_currentUserName = (await UsersService.GetUser(_currentUser)).Name;
		await Reload();
	}

	private async Task Reload()
	{
		// trigger IsLoading
		_features = null;
		Reset();

		_features = (await FeaturesService.GetFeatures()).ToList();
	}

	private void Reset() =>
		_newFeature = _editFeature = null;

	private async Task CreateFeature()
	{
		_newFeature = new()
		{
			FeatureId = FeatureId.From(0),
			FeatureType = FeatureType.None,
			Name = string.Empty,
			CreatorUserId = _currentUser,
			CreatorName = _currentUserName,
		};
		await _featuresGrid.InsertRow(_newFeature);
	}

	private async Task EditFeature(Feature feature)
	{
		_editFeature = feature;
		_editClone = feature.Clone();
		await _featuresGrid.EditRow(feature);
	}

	private Task SaveFeature(Feature feature) =>
		_featuresGrid.UpdateRow(feature);

	private async Task DeleteFeature(Feature feature)
	{
		if (feature == _newFeature)
		{
			_featuresGrid.CancelEditRow(feature);
			Reset();
		}
		else
		{
			_ = await FeaturesService.DeleteFeature(feature.FeatureId);
			_features!.Remove(feature);
			await _featuresGrid!.Reload();
		}
	}

	private Task CancelEdit(Feature feature)
	{
		_featuresGrid.CancelEditRow(feature);
		if (feature == _editFeature)
			_editClone!.CloneTo(feature);

		Reset();
		return Task.CompletedTask;
	}

	private async Task OnCreateRow(Feature feature)
	{
		var dbFeature = await FeaturesService.CreateFeature(
			new()
			{
				Name = feature.Name,
				FeatureType = feature.FeatureType,
				CreatorUserId = _currentUser,
				ValueA = feature.ValueA,
				ValueB = feature.ValueB,
			});

		Reset();
		_features!.Add(dbFeature);
		await _featuresGrid!.Reload();
	}

	private async Task OnUpdateRow(Feature feature)
	{
		_ = await FeaturesService.UpdateFeature(feature);
		Reset();
	}
}
