using Immediate.Validations.Shared;
using Microsoft.Extensions.Options;

namespace VsaTemplate.Web.Infrastructure.Startup;

public static class OptionsExtensions
{
	public static void AddRequiredOptions<T>(this IServiceCollection services, string section)
		where T : class, IValidationTarget<T>
	{
		services
			.AddOptionsWithValidateOnStart<T>()
			.Validate(
				o =>
				{
					ValidationException.ThrowIfInvalid(o, $@"Validation error for ""{typeof(T).Name}"".");
					return true;
				}
			);

		services.AddOptions<T>().BindConfiguration(section);
		services.AddTransient(sp => sp.GetRequiredService<IOptionsMonitor<T>>().CurrentValue);
	}
}
