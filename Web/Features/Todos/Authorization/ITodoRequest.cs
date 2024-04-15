using VsaTemplate.Web.Features.Todos.Models;

namespace VsaTemplate.Web.Features.Todos.Authorization;

public interface ITodoRequest
{
	TodoId TodoId { get; }
}
