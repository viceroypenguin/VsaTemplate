using System.Reflection;
using System.Text.RegularExpressions;
using LinqToDB;
using LinqToDB.Data;

namespace VsaTemplate.Web.Database;

public partial class DbContext : DataConnection
{
	public void InitializeDatabase()
	{
		_logger.LogInformation("Initializing VsaTemplate DB");

		CommandTimeout = 600;

		EnsureVersionHistoryExists();
		RunChangeScripts();
		SyncAllEnums();

		s_dbInitialized = true;
		_logger.LogInformation("VsaTemplate DB Initialized");
	}

	#region Main Functions
	private void EnsureVersionHistoryExists() =>
		// script does validation; always run script
		ExecuteScript("00.VersionHistory.sql");

	private void RunChangeScripts()
	{
		var scripts = GetEmbeddedScripts();
		var executedScripts = VersionHistories
			.Select(s => s.SqlFile)
			.ToList();

		var scriptsToRun = scripts
			.Except(executedScripts, StringComparer.OrdinalIgnoreCase)
			.Order(StringComparer.OrdinalIgnoreCase)
			.ToList();

		foreach (var s in scriptsToRun)
		{
			var startTime = DateTimeOffset.Now;
			using (var ts = BeginTransaction())
			{
				ExecuteScript(s);
				ts.Commit();
			}

			var endTime = DateTimeOffset.Now;

			_ = this.Insert(
				new Models.VersionHistory()
				{
					SqlFile = s,
					ExecutionStart = startTime,
					ExecutionEnd = endTime,
				});
		}
	}
	#endregion

	#region Execute Script
	[GeneratedRegex("^go\\r?$", RegexOptions.IgnoreCase | RegexOptions.Multiline | RegexOptions.Compiled, "en-US")]
	private static partial Regex SqlBlockRegex();

	private void ExecuteScript(string scriptName)
	{
		try
		{
			var script = EmbeddedResource.GetContent(scriptName);
			foreach (var b in SqlBlockRegex().Split(script).Where(s => !string.IsNullOrWhiteSpace(s)))
				_ = this.Execute(b);

			_logger.LogInformation($"Executed script '{scriptName}'.");
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, $"Unable to run script '{scriptName}'.");
			throw;
		}
	}
	#endregion

	#region Embedded Scripts
	private const string ResourcePrefix = "VsaTemplate.Web.Database.Scripts.";
	private static List<string> GetEmbeddedScripts() =>
		Assembly.GetExecutingAssembly()
			.GetManifestResourceNames()
			.Where(s => Path.GetExtension(s).Equals(".sql", StringComparison.OrdinalIgnoreCase))
			.Select(s => s.Replace(ResourcePrefix, "", StringComparison.OrdinalIgnoreCase))
			.ToList();

	#endregion
}
