using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.Consistart.Extensions;

[ExcludeFromCodeCoverage]
public static class MemoryCacheExtensions
{
    public static bool TryGetWithLogging<T>(
        this IMemoryCache cache,
        string cacheKey,
        ILogger logger,
        out T? cachedValue
    )
    {
        if (cache.TryGetValue(cacheKey, out cachedValue) && cachedValue != null)
        {
            logger.LogDebug(
                "Cache HIT for key {CacheKey} => {CachedValue}",
                cacheKey,
                cachedValue.GetType().Name
            );
            return true;
        }

        logger.LogDebug("Cache MISS for key {CacheKey}", cacheKey);
        cachedValue = default;
        return false;
    }

    public static void SetWithLogging<T>(
        this IMemoryCache cache,
        string cacheKey,
        T value,
        ILogger logger,
        TimeSpan? timeSpan = null
    )
    {
        cache.Set(cacheKey, value, timeSpan ?? TimeSpan.FromHours(1));
        logger.LogDebug("Cache SET for key {CacheKey}", cacheKey);
    }
}
