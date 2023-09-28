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
	private List<User>? _users;
	private User? _newUser;
	private User? _editUser;
	private User? _editClone;

	private bool IsLoading => _users == null;
	private bool IsEditing => _newUser != null || _editUser != null;

	protected override Task OnInitializedAsync() => Reload();

	private async Task Reload()
	{
		// trigger IsLoading
		_users = null;
		Reset();

		_users = (await UsersService.GetUsers()).ToList();
	}

	private void Reset() =>
		_newUser = _editUser = _editClone = null;

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
		_editClone = user.Clone();
		await _usersGrid.EditRow(user);
	}

	private Task SaveUser(User user) =>
		_usersGrid.UpdateRow(user);

	private Task CancelEdit(User user)
	{
		_usersGrid.CancelEditRow(user);
		if (user == _editUser)
			_editClone!.CloneTo(_editUser);

		Reset();
		return Task.CompletedTask;
	}

	private async Task OnCreateRow(User user)
	{
		var dbUser = await UsersService.CreateUser(
			new()
			{
				EmailAddress = user.EmailAddress,
				Name = user.Name!,
				IsActive = user.IsActive,
			});

		Reset();
		_users!.Add(dbUser);
		await _usersGrid!.Reload();
	}

	private async Task OnUpdateRow(User user)
	{
		_ = await UsersService.UpdateUser(user);

		Reset();
	}
}
