using LinqToDB;
using VsaTemplate.Web.Features.Shared.Exceptions;

namespace VsaTemplate.Web.Features.Shared.Extensions;

public static class IQueryableExtensions
{
	public static async Task<T> FirstNotFoundAsync<T>(this IQueryable<T> query, string entityName, CancellationToken token)
	{
		return await query.Take(1).ToListAsync(token) is [var item]
			? item
			: throw new NotFoundException(entityName);
	}
}
