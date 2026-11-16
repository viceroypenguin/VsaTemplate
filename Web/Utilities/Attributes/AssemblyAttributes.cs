using Immediate.Handlers.Shared;
using Immediate.Validations.Shared;
using VsaTemplate.Web.Features.AccessControl.Authorization;
using VsaTemplate.Web.Infrastructure.Logging;
using VsaTemplate.Web.Features.Tenants.Authorization;
using VsaTemplate.Web.Features.Organizations.Authorization;

[assembly: Behaviors(
	typeof(LoggingBehavior<,>),
	typeof(AuthorizationBehavior<,>),
	typeof(TenantAuthorizationBehavior<,>),
	typeof(OrganizationAuthorizationBehavior<,>),
	typeof(ValidationBehavior<,>)
)]

[assembly: VogenDefaults(
	deserializationStrictness: DeserializationStrictness.AllowAnything,
	openApiSchemaCustomizations: OpenApiSchemaCustomizations.GenerateOpenApiMappingExtensionMethod
)]
