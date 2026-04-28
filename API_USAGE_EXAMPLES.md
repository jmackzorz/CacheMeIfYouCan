# Redis Caching - API Usage Examples

## Setup (Before Running)

1. **Start Redis**
   ```powershell
   docker run -d -p 6379:6379 --name redis-dev redis:latest
   ```

2. **Update Connection String** (if needed)
   ```json
   // appsettings.json
   {
     "ConnectionStrings": {
       "Redis": "localhost:6379"
     }
   }
   ```

3. **Run Application**
   ```powershell
   dotnet run
   ```

---

## Scenario 1: First GET Request (Cache Miss)

### Request
```
GET /api/products/1
```

### Cache Flow
```
Cache key: "product:1"
  ├─ Check cache ──▶ MISS
  │
  ├─ Query database ✓
  │
  ├─ Store in cache
  │   ├─ Key: "product:1"
  │   ├─ Value: {"id":1,"name":"Product A",...}
  │   ├─ Expiry: 15 min absolute + 10 min sliding
  │
  └─ Return 200 OK
```

### Log Output
```
[Information] Getting product with ID 1
[Information] Cache miss for product ID 1 - fetching from database
[Debug] Cache set for key product:1
```

### Response
```json
{
  "id": 1,
  "name": "Product A",
  "price": 29.99,
  "categoryId": 1,
  "categoryName": "Electronics",
  "stockQuantity": 100,
  "description": "High quality product"
}
```

---

## Scenario 2: Second GET Request (Cache Hit)

### Request (within 10 minutes)
```
GET /api/products/1
```

### Cache Flow
```
Cache key: "product:1"
  ├─ Check cache ──▶ HIT
  │   ├─ Value: {"id":1,"name":"Product A",...}
  │   ├─ Sliding timer resets ✓
  │   └─ TTL: 10 min (reset from last access)
  │
  └─ Return 200 OK [< 1ms]
```

### Log Output
```
[Information] Getting product with ID 1
[Information] Cache hit for product ID 1
```

### Performance
- Database queries: 0
- Network latency: < 1 ms
- Response time: ~100x faster than scenario 1

---

## Scenario 3: GET All Products (Cache Miss)

### Request
```
GET /api/products
```

### Cache Flow
```
Cache key: "product:all_products"
  ├─ Check cache ──▶ MISS
  │
  ├─ Query database ✓
  │   └─ SELECT * FROM Products (with categories)
  │
  ├─ Store in cache
  │   ├─ Key: "product:all_products"
  │   ├─ Value: [{"id":1,...}, {"id":2,...}, ...]
  │   ├─ Expiry: 15 min absolute + 10 min sliding
  │
  └─ Return 200 OK
```

### Response
```json
[
  {
    "id": 1,
    "name": "Product A",
    "price": 29.99,
    ...
  },
  {
    "id": 2,
    "name": "Product B",
    "price": 49.99,
    ...
  }
]
```

### Log Output
```
[Information] Getting all products
[Information] Cache miss for all products - fetching from database
[Debug] Cache set for key product:all_products
```

---

## Scenario 4: Create New Product (Invalidation)

### Request
```
POST /api/products
Content-Type: application/json

{
  "name": "New Product",
  "price": 39.99,
  "categoryId": 1,
  "stockQuantity": 50
}
```

### Cache Flow
```
Database Operation
  ├─ INSERT into Products ✓
  │
  └─ Invalidate Cache
    └─ Remove: "product:all_products"
       (Individual product cache not affected)
```

### Log Output
```
[Information] Creating new product: New Product
[Debug] Cache invalidated for key product:all_products
```

### Response
```
201 Created
Location: /api/products/3

{
  "id": 3,
  "name": "New Product",
  "price": 39.99,
  ...
}
```

### Cache State After
```
Redis Keys:
  - product:1  ✓ (still cached)
  - product:2  ✓ (still cached)
  - product:all_products  ✗ (deleted)
```

---

## Scenario 5: Update Product (Full Invalidation)

### Request
```
PUT /api/products/1
Content-Type: application/json

{
  "id": 1,
  "name": "Updated Product A",
  "price": 34.99,
  "categoryId": 1,
  "stockQuantity": 75
}
```

### Cache Flow
```
Database Operation
  ├─ UPDATE Products SET ... ✓
  │
  └─ Invalidate Cache
    ├─ Remove: "product:1"
    │  (specific product cache)
    │
    └─ Remove: "product:all_products"
       (list cache - item changed)
```

### Log Output
```
[Information] Updating product with ID 1
[Debug] Cache invalidated for key product:1
[Debug] Cache invalidated for key product:all_products
```

### Response
```
204 No Content
```

### Cache State After
```
Redis Keys:
  - product:1  ✗ (deleted - will refetch on next GET)
  - product:2  ✓ (still cached)
  - product:all_products  ✗ (deleted - will refetch on next GET)
```

### Next GET Request
```
GET /api/products/1
  ├─ Cache key: "product:1" ──▶ MISS
  ├─ Query database ✓
  ├─ Get updated: {"id":1,"name":"Updated Product A","price":34.99,...}
  ├─ Store in cache
  └─ Return 200 OK
```

---

## Scenario 6: Delete Product (Full Invalidation)

### Request
```
DELETE /api/products/2
```

### Cache Flow
```
Database Operation
  ├─ DELETE FROM Products WHERE id=2 ✓
  │
  └─ Invalidate Cache
    ├─ Remove: "product:2"
    │  (product no longer exists)
    │
    └─ Remove: "product:all_products"
       (list reduced)
```

### Log Output
```
[Information] Deleting product with ID 2
[Debug] Cache invalidated for key product:2
[Debug] Cache invalidated for key product:all_products
```

### Response
```
204 No Content
```

### Cache State After
```
Redis Keys:
  - product:1  ✓ (still cached)
  - product:2  ✗ (deleted)
  - product:all_products  ✗ (deleted)
```

---

## Monitoring Cache with Redis CLI

### Check What's Cached
```powershell
# Open Redis CLI
redis-cli

# See all product keys
> KEYS product:*
1) "product:1"
2) "product:all_products"

# View specific cached product
> GET "product:1"
"{\"id\":1,\"name\":\"Product A\",\"price\":29.99,...}"

# Check TTL (seconds until expiry)
> TTL "product:1"
(integer) 590  # 9 minutes 50 seconds remaining

# Check TTL in milliseconds
> PTTL "product:1"
(integer) 590000

# Watch cache operations in real-time
> MONITOR
# Then make API calls and watch output
# (Press Ctrl+C to stop)
```

---

## Performance Comparison

### Scenario: Get Product 100 Times

#### Without Caching
```
Requests: 100
Database queries: 100
Total time: ~5000 ms (50 ms per request)
Cache hits: 0%
```

#### With Caching
```
Request 1: Cache MISS → Database hit → 50 ms
Request 2-100: Cache HIT → No database → 0.1 ms each
Total time: 50 + (99 × 0.1) ≈ 60 ms
Cache hits: 99%
Performance gain: ~83x faster
```

---

## Error Scenarios

### Scenario A: Redis Not Running

### Request
```
GET /api/products/1
```

### Log Output
```
[Error] Error setting cache for key product:1
[Information] Cache miss for product ID 1 - fetching from database
```

### Behavior
- ✅ Request still succeeds
- ✅ Data fetched from database
- ❌ Cache operation failed (non-blocking)
- ⚠️  Performance without Redis backup

### Resolution
```powershell
# Start Redis
docker run -d -p 6379:6379 redis:latest

# Retry request - should cache successfully
```

---

### Scenario B: Invalid Product ID

### Request
```
GET /api/products/999
```

### Cache Flow
```
Cache key: "product:999"
  ├─ Check cache ──▶ MISS
  ├─ Query database ──▶ NOT FOUND
  └─ Return 404 Not Found
```

### Log Output
```
[Information] Getting product with ID 999
[Information] Cache miss for product ID 999 - fetching from database
[Warning] Product ID 999 not found in database
```

### Response
```
404 Not Found
"Product with ID 999 not found"
```

### Note
- Product not cached (null not serialized)
- Next request will query database again
- Consider caching negative results for high-miss scenarios

---

## Summary

| Endpoint | Method | Cache Behavior |
|----------|--------|---|
| `/api/products/{id}` | GET | Cache-aside (hit/miss on id) |
| `/api/products` | GET | Cache-aside (single all_products key) |
| `/api/products` | POST | Invalidate all_products |
| `/api/products/{id}` | PUT | Invalidate id + all_products |
| `/api/products/{id}` | DELETE | Invalidate id + all_products |

## Next Steps

1. **Test with Redis** - Follow REDIS_SETUP.md
2. **Monitor performance** - Check logs and Redis CLI
3. **Adjust TTL** - Based on data freshness requirements
4. **Load test** - Verify cache hit ratio
5. **Add metrics** - Track cache hits/misses
