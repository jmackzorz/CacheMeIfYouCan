# Redis Setup Guide

## Option 1: Docker (Recommended for Development)

### Start Redis Container
```powershell
docker run -d -p 6379:6379 --name redis-dev redis:latest
```

### Verify Redis is Running
```powershell
docker ps | findstr redis-dev
```

### Connect to Redis CLI
```powershell
docker exec -it redis-dev redis-cli
```

### Test Redis
```
> PING
PONG

> SET test "hello"
OK

> GET test
"hello"

> FLUSHDB
OK
```

### Stop Redis Container
```powershell
docker stop redis-dev
docker rm redis-dev
```

## Option 2: Windows Native Installation

### Install using Chocolatey
```powershell
choco install redis-64
```

### Start Redis Service
```powershell
redis-server
```

### Connect with Redis CLI
```powershell
redis-cli
```

## Option 3: Windows Subsystem for Linux (WSL2)

### Install Redis in WSL
```bash
sudo apt-get update
sudo apt-get install redis-server
```

### Start Redis
```bash
redis-server
```

## Option 4: Azure Cache for Redis (Production)

### Connection String Format
```
redis-server.redis.cache.windows.net:6380,
ssl=true,
password=<your-access-key>,
abortConnect=false
```

### Update appsettings.json
```json
{
  "ConnectionStrings": {
    "Redis": "redis-prod.redis.cache.windows.net:6380,ssl=true,password=...,abortConnect=false"
  }
}
```

## Connection String Formats

### Local Development
```
localhost:6379
```

### Docker
```
redis-service:6379
```

### Azure Cache for Redis
```
cachename.redis.cache.windows.net:6380,ssl=true,password=<key>,abortConnect=false
```

### AWS ElastiCache
```
cache-endpoint.xxxxxxxxxx.cache.amazonaws.com:6379,ssl=false
```

### Docker Compose
```yaml
services:
  redis:
    image: redis:latest
    ports:
      - "6379:6379"
    volumes:
      - redis-data:/data

  api:
    build: .
    environment:
      ConnectionStrings__Redis: redis:6379
    depends_on:
      - redis

volumes:
  redis-data:
```

## Verify Cache is Working

### Method 1: Redis CLI
```powershell
# Open another terminal
redis-cli

# Monitor cache operations in real-time
> MONITOR

# Run API calls in first terminal
# You'll see cache operations in CLI

# View specific keys
> KEYS product:*
> GET "product:1"
> TTL "product:1"
```

### Method 2: Application Logs
```
[Information] Cache hit for product ID 1
[Information] Cache miss for product ID 5 - fetching from database
[Debug] Cache set for key product:5
[Debug] Cache invalidated for key product:1
```

## Debugging Cache Issues

### Issue: "Cannot connect to Redis"

**Solution 1:** Start Redis
```powershell
docker run -d -p 6379:6379 redis:latest
# or
redis-server
```

**Solution 2:** Check connection string
```json
{
  "ConnectionStrings": {
    "Redis": "localhost:6379"  // Make sure this is correct
  }
}
```

**Solution 3:** Check firewall
```powershell
# Windows: Allow port 6379
# Or use local Redis only
```

### Issue: "Cache is not working"

**Debug steps:**
1. Check Redis is running: `redis-cli ping`
2. Check logs for errors: `[Error]` messages
3. Clear cache: `redis-cli FLUSHDB`
4. Restart application
5. Make API requests and check Redis keys: `redis-cli KEYS product:*`

### Issue: "Memory error - eviction policy"

**Solution:** Set max-memory policy
```powershell
# In redis-cli
> CONFIG SET maxmemory-policy allkeys-lru
> CONFIG REWRITE
```

## Best Practices

1. **Development**
   - Use Docker for consistency
   - Flush cache between tests
   - Check logs regularly

2. **Testing**
   - Mock `IDistributedCache` for unit tests
   - Use Docker for integration tests
   - Clear cache before each test: `FLUSHDB`

3. **Production**
   - Use managed service (Azure/AWS)
   - Enable persistence (RDB/AOF)
   - Set max-memory-policy
   - Configure replication
   - Monitor memory/throughput
   - Set up alerts

4. **Security**
   - Use strong passwords
   - Enable SSL/TLS
   - Restrict network access
   - Don't expose port 6379 to internet
   - Use private subnet for Redis

## Commands Reference

```powershell
# Test connection
redis-cli ping

# View all keys
redis-cli KEYS "*"

# View keys matching pattern
redis-cli KEYS "product:*"

# Get value
redis-cli GET "product:1"

# Get expiration time (seconds until expiry)
redis-cli TTL "product:1"

# Get expiration time (milliseconds)
redis-cli PTTL "product:1"

# Delete key
redis-cli DEL "product:1"

# Clear all keys (use with caution!)
redis-cli FLUSHDB

# Get Redis info
redis-cli INFO

# Get memory usage
redis-cli INFO memory

# Monitor real-time operations
redis-cli MONITOR
```

## Monitoring Dashboard

### Redis Commander (Web UI)
```powershell
npm install -g redis-commander
redis-commander
# Open http://localhost:8081
```

### RedisInsight (Official Desktop App)
Download from: https://redis.io/insight/

### Logs Viewer in Visual Studio
Look in **Output** window → **Application** pane
```
[Information] Cache hit for product ID 1
[Debug] Cache set for key product:1
[Error] Error setting cache for key product:1
```
