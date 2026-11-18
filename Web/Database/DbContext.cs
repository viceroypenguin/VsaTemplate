using Immediate.Injections.Shared;
using Immediate.Validations.Shared;
using LinqToDB;
using LinqToDB.Data;
using LinqToDB.DataProvider.SqlServer;
using LinqToDB.Mapping;
using Microsoft.Extensions.Options;

namespace VsaTemplate.Web.Database;

[RegisterOptions(ValidateOnStart = true)]
[Validate]
public sealed partial class DbContextOptions : IValidationTarget<DbContextOptions>
{
	[NotEmpty]
	public required string ConnectionString { get; init; }

	public string? ConnectionStringInit { get; init; }

	public string? UserSourceDatabase { get; init; }
}

[RegisterTransient]
public sealed partial class DbContext(
	IOptionsSnapshot<DbContextOptions> options,
	ILogger<DbContext> logger,
	TimeProvider timeProvider,
	DatabaseInitializationState initialization
) : DataConnection(
	new DataOptions()
		.UseDataProvider(
			SqlServerTools.GetDataProvider(
				SqlServerVersion.v2025,
				SqlServerProvider.MicrosoftDataSqlClient
			)
		)
		.UseConnectionString(GetConnectionString(options.Value, initialization))
		.UseMappingSchema(MappingSchema)
)
{
	private static new readonly MappingSchema MappingSchema = BuildMappingSchema();
	private readonly DbContextOptions _options = options.Value;

	private static string GetConnectionString(DbContextOptions options, DatabaseInitializationState initialization)
	{
		var conn = options.ConnectionString;

		if (!initialization.IsInitialized && !string.IsNullOrWhiteSpace(options.ConnectionStringInit))
			conn = options.ConnectionStringInit;

		return conn;
	}

	private static partial MappingSchema BuildMappingSchema();

	public ITable<Models.AccessControl.User>? SourceUsers =>
		_options.UserSourceDatabase switch
		{
			null => null,

			{ } db => this
				.GetTable<Models.AccessControl.User>()
				.SchemaName(name: null)
				.DatabaseName(db),
		};
}

[RegisterSingleton]
public sealed class DatabaseInitializationState
{
	public bool IsInitialized { get; set; }
}
