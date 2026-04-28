using System.Text.Json;
using CacheMeIfYouCan.Application.Caching;
using CacheMeIfYouCan.Application.DTOs;
using CacheMeIfYouCan.Application.Repositories;
using CacheMeIfYouCan.Domain.Entities;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;

namespace CacheMeIfYouCan.Application.Services;

public class ProductService : IProductService
{
    private readonly IProductRepository _repo;
    private readonly IDistributedCache _cache;
    private readonly ILogger<ProductService> _logger;

    public ProductService(
        IProductRepository repo,
        IDistributedCache cache,
        ILogger<ProductService> logger)
    {
        _repo = repo;
        _cache = cache;
        _logger = logger;
    }

    public async Task<ProductDto> AddAsync(CreateProductDto dto)
    {
        var product = new Product
        {
            Name = dto.Name,
            Description = dto.Description,
            Price = dto.Price,
            CategoryId = dto.CategoryId,
            StockQuantity = dto.StockQuantity
        };

        await _repo.AddAsync(product);

        // Invalidate all products cache on new product
        await InvalidateAllProductsCacheAsync();

        return MapToDto(product);
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var product = await _repo.GetByIdAsync(id);
        if (product is null) return false;

        await _repo.DeleteAsync(id);

        // Invalidate cache for this product and all products
        await InvalidateProductCacheAsync(id);
        await InvalidateAllProductsCacheAsync();

        _logger.LogInformation("Cache invalidated for product ID {ProductId} and all products", id);

        return true;
    }

    public async Task<IEnumerable<ProductDto>> GetAllAsync()
    {
        var cacheKey = CacheKeyStrategy.GetAllProductsKey();

        // Try to get from cache
        var cachedData = await _cache.GetStringAsync(cacheKey);
        if (!string.IsNullOrEmpty(cachedData))
        {
            _logger.LogInformation("Cache hit for all products");
            var cachedProducts = JsonSerializer.Deserialize<List<ProductDto>>(cachedData);
            return cachedProducts ?? new List<ProductDto>();
        }

        _logger.LogInformation("Cache miss for all products - fetching from database");

        // Cache miss - fetch from database
        var products = await _repo.GetAllAsync();
        var productDtos = products.Select(MapToDto).ToList();

        // Store in cache
        await SetCacheAsync(cacheKey, productDtos);

        return productDtos;
    }

    public async Task<ProductDto?> GetByIdAsync(int id)
    {
        var cacheKey = CacheKeyStrategy.GetProductKey(id);

        // Try to get from cache
        var cachedData = await _cache.GetStringAsync(cacheKey);
        if (!string.IsNullOrEmpty(cachedData))
        {
            _logger.LogInformation("Cache hit for product ID {ProductId}", id);
            var cachedProduct = JsonSerializer.Deserialize<ProductDto>(cachedData);
            return cachedProduct;
        }

        _logger.LogInformation("Cache miss for product ID {ProductId} - fetching from database", id);

        // Cache miss - fetch from database
        var product = await _repo.GetByIdAsync(id);
        if (product is null)
        {
            _logger.LogWarning("Product ID {ProductId} not found in database", id);
            return null;
        }

        var productDto = MapToDto(product);

        // Store in cache
        await SetCacheAsync(cacheKey, productDto);

        return productDto;
    }

    public async Task<bool> UpdateAsync(int id, UpdateProductDto dto)
    {
        var product = await _repo.GetByIdAsync(id);
        if (product is null) return false;

        product.Name = dto.Name;
        product.Description = dto.Description;
        product.Price = dto.Price;
        product.CategoryId = dto.CategoryId;
        product.StockQuantity = dto.StockQuantity;

        await _repo.UpdateAsync(product);

        // Invalidate cache for this product and all products
        await InvalidateProductCacheAsync(id);
        await InvalidateAllProductsCacheAsync();

        _logger.LogInformation("Cache invalidated for product ID {ProductId} and all products", id);

        return true;
    }

    private static ProductDto MapToDto(Product product)
    {
        return new ProductDto
        {
            Id = product.Id,
            Name = product.Name,
            Description = product.Description,
            Price = product.Price,
            CategoryId = product.CategoryId,
            CategoryName = product.Category?.Name,
            StockQuantity = product.StockQuantity
        };
    }

    /// <summary>
    /// Set cache value with serialization
    /// </summary>
    private async Task SetCacheAsync<T>(string key, T value)
    {
        try
        {
            var serialized = JsonSerializer.Serialize(value);
            var options = CacheConfig.GetCacheOptions();
            await _cache.SetStringAsync(key, serialized, options);
            _logger.LogDebug("Cache set for key {CacheKey}", key);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting cache for key {CacheKey}", key);
            // Don't throw - cache failures shouldn't break the application
        }
    }

    /// <summary>
    /// Invalidate cache for a specific product
    /// </summary>
    private async Task InvalidateProductCacheAsync(int productId)
    {
        try
        {
            var cacheKey = CacheKeyStrategy.GetProductKey(productId);
            await _cache.RemoveAsync(cacheKey);
            _logger.LogDebug("Cache invalidated for key {CacheKey}", cacheKey);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error invalidating cache for product ID {ProductId}", productId);
            // Don't throw - cache failures shouldn't break the application
        }
    }

    /// <summary>
    /// Invalidate cache for all products
    /// </summary>
    private async Task InvalidateAllProductsCacheAsync()
    {
        try
        {
            var cacheKey = CacheKeyStrategy.GetAllProductsKey();
            await _cache.RemoveAsync(cacheKey);
            _logger.LogDebug("Cache invalidated for key {CacheKey}", cacheKey);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error invalidating all products cache");
            // Don't throw - cache failures shouldn't break the application
        }
    }
}
