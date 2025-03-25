using LinqToDB;
using VsaTemplate.Api.Features.Shared.Exceptions;

namespace VsaTemplate.Api.Features.Shared.Extensions;

public static class IQueryableExtensions
{
	public static async Task<T> FirstNotFoundAsync<T>(this IQueryable<T> query, string entityName, CancellationToken token) =>
		await query.Take(1).ToListAsync(token) switch
		{
			[var item] => item,
			_ => NotFoundException.ThrowNotFoundException<T>(entityName),
		};
}
