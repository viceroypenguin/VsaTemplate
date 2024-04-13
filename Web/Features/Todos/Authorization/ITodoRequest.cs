using VsaTemplate.Web.Features.Todos.Models;

namespace VsaTemplate.Web.Features.Todos.Authorization;

internal interface ITodoRequest
{
	TodoId TodoId { get; }
}
