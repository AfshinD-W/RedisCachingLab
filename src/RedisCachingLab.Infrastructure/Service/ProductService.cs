using Microsoft.Extensions.Logging;
using RedisCachingLab.Core.Entities;
using RedisCachingLab.Core.Interfaces;
using System.Diagnostics;

namespace RedisCachingLab.Infrastructure.Services;

public class ProductService(IProductRepository repository, ICacheService cache, ILogger<ProductService> logger) : IProductService
{
    const string cacheAllKeys = "products:all";

    public async Task<Product?> GetByIdAsync(int id)
    {
        // 1. Generate cache key
        var cacheKey = $"products:{id}";

        // 2. Check Redis
        var cachedProduct = await cache.GetAsync<Product>(cacheKey);

        // 3. Cache Hit
        if (cachedProduct is not null)
            return cachedProduct;

        // 4. Cache Miss - Get from SQL Server
        var product = await repository.GetByIdAsync(id);

        if (product is null)
            return null;

        // 5. Store in Redis
        await cache.SetAsync(cacheKey, product);

        return product;
    }

    public async Task<List<Product>> GetAllAsync()
    {
        var stopwatch = Stopwatch.StartNew();

        // 1. Redis GET
        var cachedProducts = await cache.GetAsync<List<Product>>(cacheAllKeys);

        logger.LogInformation("Redis GET completed in {ElapsedMs} ms", stopwatch.ElapsedMilliseconds);

        if (cachedProducts is not null)
            return cachedProducts;

        // 2. SQL GET
        stopwatch.Restart();

        var products = await repository.GetAllAsync();

        logger.LogInformation("SQL GET completed in {ElapsedMs} ms", stopwatch.ElapsedMilliseconds);

        // 3. Redis SET
        stopwatch.Restart();

        await cache.SetAsync(cacheAllKeys, products);

        logger.LogInformation("Redis SET completed in {ElapsedMs} ms", stopwatch.ElapsedMilliseconds);

        return products;
    }

    public async Task<Product> CreateAsync(Product product)
    {
        await repository.AddAsync(product);

        await cache.RemoveAsync(cacheAllKeys);

        return product;
    }

    public async Task UpdateAsync(Product product)
    {
        await repository.UpdateAsync(product);

        var cacheKey = $"products:{product.Id}";
        await cache.RemoveAsync(cacheKey);
        await cache.RemoveAsync(cacheAllKeys);

    }

    public async Task<bool> DeleteAsync(int id)
    {
        var product = await repository.GetByIdAsync(id);

        if (product is null)
            return false;

        await repository.DeleteAsync(product);

        var cacheKey = $"products:{id}";
        await cache.RemoveAsync(cacheKey);
        await cache.RemoveAsync(cacheAllKeys);

        return true;
    }

}
