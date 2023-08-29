using System.Reflection;
using CommunityToolkit.Diagnostics;
using VsaTemplate.Support;

namespace VsaTemplate.Web.Code;

public static class ConfigureAllOptionsExtensions
{
	public static IServiceCollection ConfigureAllOptions(
		this IServiceCollection services, IConfigurationRoot configuration)
	{
		Guard.IsNotNull(services);
		Guard.IsNotNull(configuration);

		var configureMethod = typeof(ConfigureAllOptionsExtensions).GetMethod(nameof(ConfigureOptions), BindingFlags.NonPublic | BindingFlags.Static);
		Guard.IsNotNull(configureMethod);

		foreach (var c in typeof(ConfigureOptionsAttribute).Assembly.GetTypes())
		{
			if (!c.IsClass)
				continue;

			if (c.GetCustomAttribute<ConfigureOptionsAttribute>() is not { } coa)
				continue;

			var func = configureMethod.MakeGenericMethod(c);
			func.Invoke(null, new object[] { services, configuration.GetSection(coa.SectionName ?? c.Name), });
		}

		return services;
	}

	private static void ConfigureOptions<T>(IServiceCollection services, IConfiguration configuration)
		where T : class =>
		services.Configure<T>(configuration);
}
