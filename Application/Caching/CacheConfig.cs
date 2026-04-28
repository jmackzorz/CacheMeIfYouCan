namespace CacheMeIfYouCan.Application.Caching;

/// <summary>
/// Cache expiration and policy configuration
/// </summary>
public class CacheConfig
{
    /// <summary>
    /// Absolute expiration time from creation (15 minutes)
    /// </summary>
    public static readonly TimeSpan AbsoluteExpiration = TimeSpan.FromMinutes(15);

    /// <summary>
    /// Sliding expiration time after last access (10 minutes)
    /// </summary>
    public static readonly TimeSpan SlidingExpiration = TimeSpan.FromMinutes(10);

    /// <summary>
    /// Get distributed cache entry options
    /// </summary>
    public static Microsoft.Extensions.Caching.Distributed.DistributedCacheEntryOptions GetCacheOptions()
    {
        return new Microsoft.Extensions.Caching.Distributed.DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = AbsoluteExpiration,
            SlidingExpiration = SlidingExpiration
        };
    }
}
