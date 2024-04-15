namespace VsaTemplate.Web.Features.Todos.Models;

[SyncEnum]
public enum TodoPriority
{
	None = 0,
	Low = 1,
	Mid = 2,
	High = 3,
}

[SyncEnum]
public enum TodoStatus
{
	None = 0,
	Inactive = 1,
	Active = 2,
	Completed = 3,
}
