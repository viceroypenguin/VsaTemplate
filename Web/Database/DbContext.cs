using CommunityToolkit.Diagnostics;
using LinqToDB.Data;
using LinqToDB.DataProvider.SqlServer;
using Microsoft.Extensions.Options;

namespace VsaTemplate.Web.Database;

[ConfigureOptions]
public class DbContextOptions
{
	public string ConnectionString { get; set; } = string.Empty;
	public string? ConnectionStringInit { get; set; }
}

[RegisterTransient]
public sealed partial class DbContext : DataConnection
{
	private static bool s_dbInitialized;
	private static bool s_loaded;
	private readonly ILogger<DbContext> _logger;

	public DbContext(IOptions<DbContextOptions> options, ILogger<DbContext> logger)
		: base(
			connectionString: GetConnectionString(options),
			dataProvider: SqlServerTools.GetDataProvider(
				SqlServerVersion.v2019,
				SqlServerProvider.MicrosoftDataSqlClient))
	{
		_logger = logger;

		if (s_loaded && !s_dbInitialized)
			throw new InvalidOperationException("Database must be initialized during startup.");
		s_loaded = true;
	}

	private static string GetConnectionString(IOptions<DbContextOptions> options)
	{
		Guard.IsNotNull(options);
		Guard.IsNotNullOrWhiteSpace(options.Value.ConnectionString);

		var conn = options.Value.ConnectionString;
		if (!s_dbInitialized && !string.IsNullOrWhiteSpace(options.Value.ConnectionStringInit))
			conn = options.Value.ConnectionStringInit;
		return conn;
	}
}
