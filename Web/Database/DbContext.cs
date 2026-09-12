using Immediate.Injections.Shared;
using Immediate.Validations.Shared;
using LinqToDB;
using LinqToDB.Data;
using LinqToDB.DataProvider.SqlServer;
using LinqToDB.Mapping;
using Microsoft.Extensions.Options;
using VsaTemplate.Web.Utilities.Attributes;

namespace VsaTemplate.Web.Database;

[ConfigureOptions]
[Validate]
public sealed partial class DbContextOptions : IValidationTarget<DbContextOptions>
{
	[NotEmpty]
	public required string ConnectionString { get; init; }

	public string? ConnectionStringInit { get; init; }
}

[RegisterTransient]
public sealed partial class DbContext : DataConnection
{
	private static new readonly MappingSchema MappingSchema = BuildMappingSchema();

	private static bool IsInitialized;
	private static bool IsLoaded;
	private readonly ILogger<DbContext> _logger;

	public DbContext(IOptions<DbContextOptions> options, ILogger<DbContext> logger)
		: base(
			new DataOptions()
				.UseDataProvider(
					SqlServerTools.GetDataProvider(
						SqlServerVersion.v2025,
						SqlServerProvider.MicrosoftDataSqlClient
					)
				)
				.UseConnectionString(GetConnectionString(options))
				.UseMappingSchema(MappingSchema)
		)
	{
		_logger = logger;

		if (IsLoaded && !IsInitialized)
			throw new InvalidOperationException("Database must be initialized during startup.");

		IsLoaded = true;
	}

	private static string GetConnectionString(IOptions<DbContextOptions> options)
	{
		var conn = options.Value.ConnectionString;
		if (!IsInitialized && !string.IsNullOrWhiteSpace(options.Value.ConnectionStringInit))
			conn = options.Value.ConnectionStringInit;

		return conn;
	}

	private static partial MappingSchema BuildMappingSchema();
}
