namespace VsaTemplate.Api.Database;

public static class DatabaseStartupExtensions
{
	public static IApplicationBuilder InitializeDatabase(this IApplicationBuilder app)
	{
		using var scope = app.ApplicationServices.CreateScope();
		var db = scope.ServiceProvider.GetRequiredService<DbContext>();
		db.InitializeDatabase();
		return app;
	}
}
