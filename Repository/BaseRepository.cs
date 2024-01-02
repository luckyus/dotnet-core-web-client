using System.Collections.Generic;
using System;
using Microsoft.Extensions.Caching.Memory;

namespace dotnet_core_web_client.Repository
{
	public class BaseRepository(IMemoryCache memoryCache)
	{
		protected readonly IMemoryCache _memoryCache = memoryCache;

		// keep track of all cache keys (230802)
		// - this is not a good method, since it won't clear the expired cache keys (230802)
		protected static readonly List<string> _cacheKeys = new();

		private readonly MemoryCacheEntryOptions memoryCacheEntryOptions = new()
		{
			AbsoluteExpiration = null,
			SlidingExpiration = TimeSpan.FromMinutes(60)
		};

		public void SetCache<T>(string cacheKey, T value)
		{
			_memoryCache.Set(cacheKey, value, memoryCacheEntryOptions);

			if (!_cacheKeys.Contains(cacheKey))
			{
				_cacheKeys.Add(cacheKey);
			}
		}

        public void ResetCache(string cacheKeyPrefix)
		{
			foreach (var cacheKey in _cacheKeys)
			{
				if (cacheKey.StartsWith(cacheKeyPrefix))
				{
					_memoryCache.Remove(cacheKey);
				}
			}

			_cacheKeys.RemoveAll(x => x.StartsWith(cacheKeyPrefix));
		}
	}
}
