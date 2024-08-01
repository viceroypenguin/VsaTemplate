using VsaTemplate.Api.Features.Todos.Models;

namespace VsaTemplate.Api.Features.Todos.Authorization;

public interface ITodoRequest
{
	TodoId TodoId { get; }
}
