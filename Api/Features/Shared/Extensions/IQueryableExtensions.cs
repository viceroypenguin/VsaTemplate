using LinqToDB;
using VsaTemplate.Api.Features.Shared.Exceptions;

namespace VsaTemplate.Api.Features.Shared.Extensions;

public static class IQueryableExtensions
{
	public static async Task<T> FirstNotFoundAsync<T>(this IQueryable<T> query, string entityName, CancellationToken token)
	{
		try
		{
			return await query.FirstAsync(token);
		}
		catch (InvalidOperationException ex) when (ex.Message is "The source sequence is empty." or "Sequence contains no elements")
		{
			throw new NotFoundException(entityName);
		}
	}
}
