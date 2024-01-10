using Microsoft.AspNetCore.Mvc;
using VsaTemplate.Web.Features.Users.Handlers;

namespace VsaTemplate.Web.Features.Users.Endpoints;

public static class Endpoints
{
	public static void MapUserEndpoints(this WebApplication app) =>
		app.MapGroup("/users")
			.MapUserEndpoints();


	public static void MapUserEndpoints(this RouteGroupBuilder group)
	{
		group
			.MapGet("", async (
				[FromServices] GetUsers.Handler handler,
				CancellationToken token
			) => await handler.HandleAsync(new(), token))
			.WithName("GetUsers")
			.WithOpenApi();
	}
}
