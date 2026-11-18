using LinqToDB;
using LinqToDB.Data;

namespace VsaTemplate.Web.Database;

public partial class DbContext : DataConnection, IVersionedDbContext
{
	public IReadOnlyList<string> GetExecutedScripts() =>
		VersionHistories
			.Select(vh => vh.SqlFile)
			.ToList();

	public void RecordExecutedScript(
		string sqlName,
		DateTimeOffset startTimestamp,
		DateTimeOffset endTimestamp
	)
	{
		this.Insert(
			new Models.VersionHistory
			{
				SqlFile = sqlName,
				ExecutionStart = startTimestamp,
				ExecutionEnd = endTimestamp,
			}
		);
	}

	public void InitializeDatabase()
	{
		this.Initialize(logger, timeProvider);

		SyncAllEnums();
		initialization.IsInitialized = true;
	}

	partial void SyncAllEnums();
}
