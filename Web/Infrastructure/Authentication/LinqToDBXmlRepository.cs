using System.Xml.Linq;
using Immediate.Cache.Shared;
using Immediate.Injections.Shared;
using LinqToDB;
using LinqToDB.Async;
using Microsoft.AspNetCore.DataProtection.Repositories;
using VsaTemplate.Web.Database;

namespace VsaTemplate.Web.Infrastructure.Authentication;

[RegisterSingleton]
public sealed class LinqToDBXmlRepository(
	Owned<DbContext> ownedContext
) : IXmlRepository
{
	public IReadOnlyCollection<XElement> GetAllElements()
	{
		return Core().GetAwaiter().GetResult();

		async Task<IReadOnlyCollection<XElement>> Core()
		{
			await using var scope = ownedContext.GetScope(out var context);

			return await context.DataProtectionKeys.DataProtectionKeys
				.Select(dpk => dpk.Xml)
				.Where(xml => xml != null)
				.Select(xml => XElement.Parse(xml!))
				.ToListAsync();
		}
	}

	public void StoreElement(XElement element, string friendlyName)
	{
		Core().GetAwaiter().GetResult();

		async Task Core()
		{
			await using var scope = ownedContext.GetScope(out var context);

			await context.InsertAsync(
				new Database.Models.DataProtectionKeys.DataProtectionKey()
				{
					FriendlyName = friendlyName,
					Xml = element.ToString(SaveOptions.DisableFormatting),
				}
			);
		}
	}
}
