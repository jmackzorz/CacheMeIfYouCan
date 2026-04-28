# Redis Caching Implementation Guide

## Overview
This document describes the Redis caching implementation in the ProductService using the **cache-aside pattern**.

## Architecture

### Cache-Aside Pattern (Lazy Loading)
1. **Request arrives** → Check cache
2. **Cache hit** → Return cached data
3. **Cache miss** → Fetch from database → Store in cache → Return data
4. **Update/Delete** → Update DB → Invalidate cache

```
┌─────────────┐
│   Request   │
└──────┬──────┘
       │
       ▼
┌──────────────┐      YES       ┌──────────┐
│ Cache Hit?   ├──────────────▶ │ Return   │
└──────┬───────┘                 │ Cached   │
       │ NO                      │ Data     │
       │                         └──────────┘
       ▼
┌──────────────┐
│  Fetch from  │
│   Database   │
└──────┬───────┘
       │
       ▼
┌──────────────┐
│ Store in     │
│   Cache      │
└──────┬───────┘
       │
       ▼
┌──────────────┐
│   Return     │
│     Data     │
└──────────────┘
```

## Cache Key Strategy

### Key Format
```
product:{productId}          // Single product: "product:1", "product:5"
product:all_products         // All products: "product:all_products"
```

**Benefits:**
- Hierarchical key structure
- Easy to identify related cache entries
- Prevents key collisions
- Supports bulk invalidation if needed

### Implementation
- File: `CacheMeIfYouCan/Application/Caching/CacheKeyStrategy.cs`
- Methods:
  - `GetProductKey(int productId)` → "product:{id}"
  - `GetAllProductsKey()` → "product:all_products"
  - `TryGetProductIdFromKey(string key, out int productId)` → Parse key safely

## Expiration Policy

### Configuration
- File: `CacheMeIfYouCan/Application/Caching/CacheConfig.cs`

```csharp
AbsoluteExpiration: 15 minutes  // Max lifetime from creation
SlidingExpiration:  10 minutes  // Reset on access
```

### Behavior
- **Entry created** at 12:00 PM
- **Entry accessed** at 12:05 PM
  - Sliding window resets: expires at 12:20 PM (12:05 + 10 min)
  - Absolute stays: expires at 12:15 PM (12:00 + 15 min)
- **Entry expires** at 12:15 PM (whichever comes first)

**Rationale:**
- Sliding: Keeps frequently accessed products cached longer
- Absolute: Ensures data doesn't stay stale indefinitely
- Combined: Balance between performance and data freshness

## Cache Invalidation Strategy

### Invalidation Triggers

#### 1. `GetByIdAsync(id)`
- **Cache hit:** Return cached data
- **Cache miss:** Fetch from DB, store in cache

#### 2. `GetAllAsync()`
- **Cache hit:** Return cached list
- **Cache miss:** Fetch from DB, store in cache

#### 3. `AddAsync(dto)` - Create Product
- Invalidates: `product:all_products`
- Reason: New product should appear in all products list

#### 4. `UpdateAsync(id, dto)` - Update Product
- Invalidates:
  - `product:{id}` (updated product)
  - `product:all_products` (list may change)
- Reason: Product data or list composition changed

#### 5. `DeleteAsync(id)` - Delete Product
- Invalidates:
  - `product:{id}` (product no longer exists)
  - `product:all_products` (list reduced)
- Reason: Product removed from system

### Invalidation Logic
```csharp
// Single product cache
await _cache.RemoveAsync(CacheKeyStrategy.GetProductKey(productId));

// All products cache
await _cache.RemoveAsync(CacheKeyStrategy.GetAllProductsKey());
```

## Serialization

### Technology: System.Text.Json
- **Built-in:** No external dependencies
- **Performance:** Native .NET, optimized for modern scenarios
- **Null handling:** Respects nullable reference types

### Implementation
```csharp
// Serialize
var json = JsonSerializer.Serialize(productDto);
await _cache.SetStringAsync(key, json, options);

// Deserialize
var cached = await _cache.GetStringAsync(key);
var product = JsonSerializer.Deserialize<ProductDto>(cached);
```

## Logging

All cache operations are logged for monitoring:

```
[Information] Cache hit for product ID 5
[Information] Cache miss for product ID 5 - fetching from database
[Debug] Cache set for key product:5
[Debug] Cache invalidated for key product:5
[Error] Error setting cache for key product:5
```

**Log Levels:**
- `Information`: Cache hits/misses at operation level
- `Debug`: Cache set/remove operations
- `Warning`: Product not found
- `Error`: Exceptions (non-blocking)

## Error Handling

### Resilience
- Cache failures **don't break** the application
- Exceptions are **caught and logged**
- Database fallback is **automatic**

```csharp
try
{
    await SetCacheAsync(key, value);
}
catch (Exception ex)
{
    _logger.LogError(ex, "Error setting cache");
    // Continue - data was saved to DB
}
```

## Configuration in Program.cs

```csharp
// Register Redis Distributed Cache
builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = redisConnectionString;
});

// Register Service with DI
builder.Services.AddScoped<IProductService, ProductService>();
```

## Connection String

**appsettings.json:**
```json
{
  "ConnectionStrings": {
    "Redis": "localhost:6379"
  }
}
```

**Formats supported:**
- `localhost:6379` - Local Redis
- `redis-server:6379` - Docker container
- `redis-prod.redis.cache.windows.net:6380,ssl=true,password=...` - Azure Cache for Redis

## Performance Characteristics

### Cache Hit
- Database: ❌ (skipped)
- Network round trip: 1 (cache only)
- Typical latency: < 1 ms
- Improvement: **100-1000x faster**

### Cache Miss
- Database: ✅ (required)
- Network round trips: 2 (cache check + DB fetch + cache set)
- Typical latency: 10-50 ms
- On subsequent requests: Cache hit (amortized cost ≈ 0)

## Testing Considerations

### Cache Behavior Verification
```csharp
// First call: Cache miss
var product1 = await service.GetByIdAsync(1);

// Second call: Cache hit
var product2 = await service.GetByIdAsync(1);

// Update: Invalidates
await service.UpdateAsync(1, updateDto);

// Third call: Cache miss again
var product3 = await service.GetByIdAsync(1);
```

### Unit Testing
- Mock `IDistributedCache` in tests
- Verify cache is checked before DB calls
- Verify invalidation on write operations
- Test serialization/deserialization

## Best Practices

1. ✅ **Always use cache-aside** - Lazy loading is safer than cache-through
2. ✅ **Invalidate on writes** - Prevent stale data
3. ✅ **Use hierarchical keys** - Easier management
4. ✅ **Set expiration** - Balance freshness vs. performance
5. ✅ **Log cache operations** - Monitor and debug
6. ✅ **Handle failures gracefully** - Cache ≠ critical path
7. ✅ **Use typed DTOs** - JSON serialization reliability
8. ✅ **Combine expiration types** - Absolute + sliding

## Troubleshooting

### Cache not working?
1. Check Redis connection: `redis-cli ping`
2. Check logs for `[Error]` messages
3. Verify connection string in `appsettings.json`
4. Ensure Redis server is running

### Stale data?
1. Reduce absolute/sliding expiration times
2. Check invalidation logic in Update/Delete
3. Verify all write paths call invalidation

### Memory issues?
1. Reduce cache duration
2. Reduce cache key scope
3. Monitor Redis memory usage
4. Set Redis max-memory policy

## Future Enhancements

1. **Cache warming** - Pre-load popular products
2. **Cache statistics** - Track hit/miss ratio
3. **Distributed invalidation** - Multi-server cache sync
4. **Compression** - For large cached objects
5. **Cache versioning** - Handle schema changes gracefully
