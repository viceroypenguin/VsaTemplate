using LinqToDB;
using VsaTemplate.Api.Features.Shared.Exceptions;

namespace VsaTemplate.Api.Features.Shared.Extensions;

public static class IQueryableExtensions
{
	public static async Task<T> FirstNotFoundAsync<T>(this IQueryable<T> query, string entityName, CancellationToken token)
	{
		return await query.Take(1).ToListAsync(token) is [var item]
			? item
			: throw new NotFoundException(entityName);
	}
}
