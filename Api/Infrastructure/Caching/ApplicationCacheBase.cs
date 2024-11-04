using System.Diagnostics.CodeAnalysis;
using Immediate.Handlers.Shared;
using Microsoft.Extensions.Caching.Memory;
using VsaTemplate.Api.Infrastructure.DependencyInjection;

namespace VsaTemplate.Api.Infrastructure.Caching;

public abstract class ApplicationCacheBase<TRequest, TResponse>(
	IMemoryCache memoryCache,
	Owned<IHandler<TRequest, TResponse>> handler
) where TResponse : class
{
	private readonly object _lock = new();

	protected abstract string TransformKey(TRequest request);

	protected virtual MemoryCacheEntryOptions GetCacheEntryOptions() =>
		new()
		{
			SlidingExpiration = TimeSpan.FromMinutes(5),
		};

	private CacheValue GetCacheValue(TRequest request)
	{
		var key = TransformKey(request);

		if (!memoryCache.TryGetValue(key, out var result))
		{
			lock (_lock)
			{
				if (!memoryCache.TryGetValue(key, out result))
				{
					using var entry = memoryCache.CreateEntry(key)
						.SetOptions(GetCacheEntryOptions());

					result = new CacheValue(request, handler);
					entry.Value = result;
				}
			}
		}

		return (CacheValue)result!;
	}

	public ValueTask<TResponse> GetValue(TRequest request) =>
		GetCacheValue(request).GetValue();

	public void SetValue(TRequest request, TResponse precomputedResponse) =>
		GetCacheValue(request).SetValue(precomputedResponse);

	[SuppressMessage("Design", "CA1001:Types that own disposable fields should be disposable")]
	private sealed class CacheValue(
		TRequest request,
		Owned<IHandler<TRequest, TResponse>> handler
	)
	{
		private TResponse? _response;
		private CancellationTokenSource? _tokenSource;
		private readonly object _lock = new();

		public async ValueTask<TResponse> GetValue()
		{
			if (_response is not null)
				return _response;

			lock (_lock)
				_tokenSource ??= new CancellationTokenSource();

			var token = _tokenSource.Token;

			try
			{
				await using var scope = handler.GetScope();
				var response = await scope.Value.HandleAsync(
					request,
					cancellationToken: token
				);

				lock (_lock)
					return _response ??= response;
			}
			catch (OperationCanceledException) when (_response is not null)
			{
				return _response;
			}
		}

		public void SetValue(TResponse response)
		{
			lock (_lock)
			{
				_response = response;
				_tokenSource?.Cancel();
			}
		}
	}
}
