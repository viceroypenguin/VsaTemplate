using CommunityToolkit.Diagnostics;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Radzen.Blazor;
using VsaTemplate.Users.Models;
using VsaTemplate.Users.Services;
using VsaTemplate.Web.Code;

namespace VsaTemplate.Web.Pages.Admin.Users;

public partial class Index : BlazorComponentBase
{
	[InjectScoped]
	public required UsersService UsersService { get; set; }

	[CascadingParameter]
	public required Task<AuthenticationState> AuthenticationState { get; set; }

	private RadzenDataGrid<User> _usersGrid = default!;
	private IReadOnlyList<User>? _users;
	private User? _newUser;
	private User? _editUser;
	private UserId _currentUser;

	private bool IsLoading => _users == null;
	private bool IsEditing => _newUser != null || _editUser != null;

	protected override async Task OnInitializedAsync()
	{
		var authState = await AuthenticationState;
		var user = authState.User;
		var claim = user.Claims.FirstOrDefault(c => c.Type == "dbid");
		Guard.IsNotNull(claim);
		Guard.IsNotNullOrWhiteSpace(claim.Value);

		_currentUser = UserId.From(int.Parse(claim.Value, provider: null));

		await Reload();
	}

	private async Task Reload()
	{
		// trigger IsLoading
		_users = null;
		Reset();

		_users = await UsersService.GetUsers();
	}

	private void Reset() =>
		_newUser = _editUser = null;

	private async Task CreateUser()
	{
		_newUser = new()
		{
			UserId = UserId.From(0),
			EmailAddress = string.Empty,
			IsActive = true,
		};
		await _usersGrid.InsertRow(_newUser);
	}

	private async Task EditUser(User user)
	{
		_editUser = user;
		await _usersGrid.EditRow(user);
	}

	private Task SaveUser(User user) =>
		_usersGrid.UpdateRow(user);

	private async Task CancelEdit(User user)
	{
		_usersGrid.CancelEditRow(user);
		if (user == _editUser)
			await Reload();
	}

	private async Task OnCreateRow(User user)
	{
		_ = await UsersService.CreateUser(
			new()
			{
				EmailAddress = user.EmailAddress,
				Name = user.Name!,
				IsActive = user.IsActive,
			});

		await Reload();
	}

	private async Task OnUpdateRow(User user)
	{
		_ = await UsersService.UpdateUser(user);
		await Reload();
	}
}
