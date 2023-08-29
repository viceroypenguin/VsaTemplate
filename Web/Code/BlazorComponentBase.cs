using System.Reflection;
using Microsoft.AspNetCore.Components;

namespace VsaTemplate.Web.Code;

public class BlazorComponentBase : OwningComponentBase
{
	protected override void OnInitialized()
	{
		var properties = this.GetType().GetProperties()
			.Where(p => p.GetCustomAttribute<InjectScopedAttribute>() != null);

		foreach (var p in properties)
			p.SetValue(this, ScopedServices.GetService(p.PropertyType));
	}
}
