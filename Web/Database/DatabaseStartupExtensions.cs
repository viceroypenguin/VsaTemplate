using LinqToDB;
using VsaTemplate.Web.Features.AccessControl.Models;
using VsaTemplate.Web.Features.AccessControl.Queries;

namespace VsaTemplate.Web.Database;

public static partial class DatabaseStartupExtensions
{
	public static async Task<IApplicationBuilder> InitializeDatabase(this IApplicationBuilder app)
	{
		await using var scope = app.ApplicationServices.CreateAsyncScope();
		var sp = scope.ServiceProvider;

		var db = sp.GetRequiredService<DbContext>();
		db.InitializeDatabase();

		var rootUser = sp.GetRequiredService<IConfiguration>()["OVERRIDEROOTUSER"];
		if (!string.IsNullOrWhiteSpace(rootUser))
		{
			var logger = sp.GetRequiredService<ILoggerFactory>().CreateLogger("OverrideRootUser");
			ReportRootUserSetup(logger, rootUser);

			var getUserId = sp.GetRequiredService<GetUserId.Handler>();
			var rootUserId = await getUserId.HandleAsync(
				new()
				{
					Auth0UserId = Auth0UserId.From(""),
					EmailAddress = rootUser,
				}
			);

			await db.RoleUsers
				.Merge().Using([new { UserId = rootUserId, RoleId = RoleId.From(-1) }])
				.On((dst, src) => dst.UserId == src.UserId && dst.RoleId == src.RoleId)
				.InsertWhenNotMatched(src => new()
				{
					UserId = src.UserId,
					RoleId = src.RoleId,
					EditedUserId = UserId.From(-1),
				})
				.MergeAsync();
		}

		return app;
	}

	[LoggerMessage("Ensuring Root User {EmailAddress} has admin access.", Level = LogLevel.Information)]
	private static partial void ReportRootUserSetup(ILogger logger, string emailAddress);
}
