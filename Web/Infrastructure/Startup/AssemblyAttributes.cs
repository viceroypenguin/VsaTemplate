using Immediate.Handlers.Shared;
using VsaTemplate.Web.Infrastructure.Behaviors;

[assembly: Behaviors(
	typeof(LoggingBehavior<,>),
	typeof(AuthorizationBehavior<,>)
)]

[assembly: VogenDefaults(conversions: Conversions.Default | Conversions.LinqToDbValueConverter)]
