using Immediate.Handlers.Shared;
using Immediate.Validations.Shared;
using VsaTemplate.Api.Features.Todos.Authorization;
using VsaTemplate.Api.Infrastructure.Behaviors;

[assembly: Behaviors(
	typeof(LoggingBehavior<,>),
	typeof(ValidationBehavior<,>),
	typeof(TodoAuthorizationBehavior<,>)
)]

[assembly: VogenDefaults(
	conversions: Conversions.Default | Conversions.LinqToDbValueConverter,
	deserializationStrictness: DeserializationStrictness.AllowAnything
)]
