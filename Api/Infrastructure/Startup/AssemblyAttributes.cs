using Immediate.Handlers.Shared;
using VsaTemplate.Api.Features.Todos.Authorization;
using VsaTemplate.Api.Infrastructure.Behaviors;

[assembly: Behaviors(
	typeof(LoggingBehavior<,>),
	typeof(TodoAuthorizationBehavior<,>)
)]

[assembly: VogenDefaults(conversions: Conversions.Default | Conversions.LinqToDbValueConverter)]
