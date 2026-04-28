namespace CacheMeIfYouCan.Application.Caching;

/// <summary>
/// Cache key strategy for Redis caching
/// </summary>
public static class CacheKeyStrategy
{
    private const string ProductPrefix = "product";
    private const string AllProductsKey = "all_products";

    /// <summary>
    /// Generate cache key for a single product
    /// </summary>
    public static string GetProductKey(int productId) => $"{ProductPrefix}:{productId}";

    /// <summary>
    /// Generate cache key for all products
    /// </summary>
    public static string GetAllProductsKey() => $"{ProductPrefix}:{AllProductsKey}";

    /// <summary>
    /// Get product ID from cache key
    /// </summary>
    public static bool TryGetProductIdFromKey(string key, out int productId)
    {
        productId = 0;
        if (!key.StartsWith($"{ProductPrefix}:"))
            return false;

        var idPart = key.Substring($"{ProductPrefix}:".Length);
        return int.TryParse(idPart, out productId);
    }
}
