using Immediate.Handlers.Shared;
using Immediate.Validations.Shared;
using VsaTemplate.Web.Infrastructure.Authorization;
using VsaTemplate.Web.Infrastructure.Logging;

[assembly: Behaviors(
	typeof(LoggingBehavior<,>),
	typeof(AuthorizationBehavior<,>),
	typeof(ValidationBehavior<,>)
)]

[assembly: VogenDefaults(
	conversions: Conversions.Default | Conversions.LinqToDbValueConverter,
	deserializationStrictness: DeserializationStrictness.AllowAnything,
	openApiSchemaCustomizations: OpenApiSchemaCustomizations.GenerateOpenApiMappingExtensionMethod
)]
