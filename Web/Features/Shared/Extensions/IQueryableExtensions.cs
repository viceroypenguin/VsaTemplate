using LinqToDB;
using LinqToDB.Async;
using VsaTemplate.Web.Features.Shared.Exceptions;

namespace VsaTemplate.Web.Features.Shared.Extensions;

public static class IQueryableExtensions
{
	public static async Task<T> FirstNotFoundAsync<T>(
		this IQueryable<T> query,
		string entityName,
		CancellationToken token
	)
	{
		var items = await query
			.Take(1)
			.ToListAsync(token);

		return items switch
		{
			[var item] => item,
			_ => NotFoundException.ThrowNotFoundException<T>(entityName),
		};
	}
}
