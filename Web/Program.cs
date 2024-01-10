using System.Diagnostics;
using System.Security.Claims;
using Auth0.AspNetCore.Authentication;
using CommunityToolkit.Diagnostics;
using DryIoc;
using DryIoc.Microsoft.DependencyInjection;
using Hangfire;
using Hangfire.SqlServer;
using Serilog;
using Serilog.Events;
using Serilog.Exceptions;
using Serilog.Exceptions.Core;
using Serilog.Exceptions.Destructurers;
using Serilog.Exceptions.MsSqlServer.Destructurers;
using Serilog.Exceptions.Refit.Destructurers;
using VsaTemplate.Users.Models;
using VsaTemplate.Users.Services;
using VsaTemplate.Web.Features.Users.Endpoints;
using VsaTemplate.Web.Infrastructure.Hangfire;
using VsaTemplate.Web.Infrastructure.Startup;

#pragma warning disable CA1852 // Type can be sealed because it has no subtypes in its containing assembly and is not externally visible

Log.Logger = new LoggerConfiguration()
	.WriteTo.Console(formatProvider: null)
	.CreateBootstrapLogger();

try
{
	var builder = WebApplication.CreateBuilder(args);

	builder.Configuration.AddJsonFile("secrets.json", optional: true);

	using var container = new Container();
	builder.Host.UseServiceProviderFactory(
		new DryIocServiceProviderFactory(container));

	builder.Host.UseSerilog((ctx, lc) => lc
		.ReadFrom.Configuration(ctx.Configuration)
		.Enrich.WithProperty("ExecutionId", Guid.NewGuid())
		.Enrich.WithProperty("Commit", ThisAssembly.Git.Commit)
		.Enrich.WithExceptionDetails(new DestructuringOptionsBuilder()
			.WithDefaultDestructurers()
			.WithDestructurers(new IExceptionDestructurer[]
			{
				new SqlExceptionDestructurer(),
				new ApiExceptionDestructurer(),
			})));

	builder.Services.AddHostedService<DatabaseInitializationService>();

	builder.Services.AddHangfire(configuration => configuration
		.UseFilter(new HangfireJobIdEnricher())
		.SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
		.UseSimpleAssemblyNameTypeSerializer()
		.UseRecommendedSerializerSettings()
		.UseSqlServerStorage(
			BuildHangfireConnectionString(builder.Configuration["DbContextOptions:ConnectionString"]!),
			new SqlServerStorageOptions
			{
				CommandBatchMaxTimeout = TimeSpan.FromMinutes(5),
				SlidingInvisibilityTimeout = TimeSpan.FromMinutes(5),
				QueuePollInterval = TimeSpan.Zero,
				UseRecommendedIsolationLevel = true,
				DisableGlobalLocks = true,
				PrepareSchemaIfNecessary = false,
			}));

	builder.Services.AddHangfireServer();
	builder.Services.AddHostedService<HangfireInitializationService>();

	builder.Services.ConfigureAllOptions();

	builder.Services.AddAuthorizationBuilder()
		.AddPolicy("has_dbid", policy => policy.RequireClaim("dbid"));

	builder.Services
		.AddAuth0WebAppAuthentication(o =>
		{
			var domain = builder.Configuration["Auth0:Domain"];
			Guard.IsNotNull(domain);
			o.Domain = domain;

			var clientId = builder.Configuration["Auth0:ClientId"];
			Guard.IsNotNull(clientId);
			o.ClientId = clientId;

			o.Scope = "openid profile email";

			o.OpenIdConnectEvents = new()
			{
				OnTicketReceived = async ctx =>
				{
					var user = ctx.Principal;
					if (user == null)
						ThrowHelper.ThrowInvalidOperationException("Got a ticket, but no valid user attached.");

					var auth0Id = user.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
					if (string.IsNullOrWhiteSpace(auth0Id))
						ThrowHelper.ThrowInvalidOperationException("Completed Auth0 login, but no Auth0 Id present.");

					var emailAddress = user.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;
					if (string.IsNullOrWhiteSpace(emailAddress))
						ThrowHelper.ThrowInvalidOperationException("Completed Auth0 login, but no email address present.");

					var usersService = ctx.HttpContext.RequestServices.GetRequiredService<UsersService>();
					var userId = await usersService.GetOrCreateUserId(Auth0UserId.From(auth0Id), emailAddress);

					user.AddIdentity(
						new ClaimsIdentity(
							new Claim[]
							{
								new("dbid", FormattableString.Invariant($"{userId.Value}")),
							}));
				},
			};
		});

	builder.Services.AutoRegisterFromServices();
	builder.Services.AddHandlers();
	builder.Services.AddBehaviors();

	builder.Services.AddEndpointsApiExplorer();
	builder.Services.AddSwaggerGen();

	builder.Services.AddHttpContextAccessor();
	builder.Services.AddAntiforgery();

	builder.Services.AddResponseCompression(options =>
	{
		options.EnableForHttps = true;
	});

	var app = builder.Build();

	app.UseHttpsRedirection();

	app.UseSwagger();
	app.UseSwaggerUI();

	app.UseStaticFiles();

	app.UseRouting();

	app.UseAuthorization();

	app.UseHangfireDashboard(
		"/hangfire",
		new DashboardOptions
		{
			Authorization =
			[
				new RolesBasedAuthorizationFilter(["Admin"]),
			],
		});

	app.UseSerilogRequestLogging(o =>
	{
		o.GetLevel = static (httpContext, _, _) =>
			httpContext.Response.StatusCode >= 500 ? LogEventLevel.Error : LogEventLevel.Information;

		o.EnrichDiagnosticContext = static (diagnosticContext, httpContext) =>
		{
			diagnosticContext.Set("User", httpContext.User?.Identity?.Name);
			diagnosticContext.Set("RemoteIP", httpContext.Connection.RemoteIpAddress);
			diagnosticContext.Set("ConnectingIP", httpContext.Request.Headers["CF-Connecting-IP"]);
		};
	});

	app.UseAntiforgery();

	app.MapUserEndpoints();

	app.Run();
}
catch (Exception ex) when (
	ex is not HostAbortedException
	&& !ex.GetType().Name.Equals("StopTheHostException", StringComparison.Ordinal))
{
	Log.Fatal(ex, "Unhandled exception");
}
finally
{
	if (new StackTrace().FrameCount == 1)
	{
		Log.Information("Shut down complete");
		Log.CloseAndFlush();
	}
}

static string BuildHangfireConnectionString(string connectionString)
{
	var builder = new Microsoft.Data.SqlClient.SqlConnectionStringBuilder(connectionString);
	builder.ApplicationName = builder.ApplicationName.Replace("VsaTemplate", "Hangfire", StringComparison.OrdinalIgnoreCase);
	builder.Remove("MultipleActiveResultSets");
	return builder.ConnectionString;
}
