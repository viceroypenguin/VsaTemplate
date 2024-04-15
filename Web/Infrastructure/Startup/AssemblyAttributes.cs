using Immediate.Handlers.Shared;
using VsaTemplate.Web.Features.Todos.Authorization;
using VsaTemplate.Web.Infrastructure.Behaviors;

[assembly: Behaviors(
	typeof(LoggingBehavior<,>),
	typeof(AuthorizationBehavior<,>),
	typeof(TodoAuthorizationBehavior<,>)
)]

[assembly: VogenDefaults(conversions: Conversions.Default | Conversions.LinqToDbValueConverter)]
