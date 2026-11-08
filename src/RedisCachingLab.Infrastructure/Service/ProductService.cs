using RedisCachingLab.Core.Entities;
using RedisCachingLab.Core.Interfaces;
using System.Collections.Concurrent;
using System.Text.Json;

namespace RedisCachingLab.Infrastructure.Services;

public class ProductService(IProductRepository repository, ICacheService cache) : IProductService
{
    private static readonly SemaphoreSlim _cacheLock = new(1, 1);
    private static readonly ConcurrentDictionary<int, SemaphoreSlim> _productLocks = new();

    private static SemaphoreSlim GetProductLock(int productId)
    {
        return _productLocks.GetOrAdd(productId, _ => new SemaphoreSlim(1, 1));
    }

    const string cacheAllKeys = "products:all";

    public async Task<Product?> GetByIdAsync(int id)
    {
        var productLock = GetProductLock(id);

        await productLock.WaitAsync();

        try
        {
            var cacheKey = $"products:{id}";

            var cachedProduct = await cache.GetAsync<Product>(cacheKey);

            if (cachedProduct is not null)
                return cachedProduct;

            var product = await repository.GetByIdAsync(id);

            if (product is null)
                return null;

            await cache.SetAsync(cacheKey, product);

            return product;
        }
        finally
        {
            productLock.Release();
        }
    }

    public async Task<List<Product>> GetAllAsync()
    {
        var cachedProducts = await cache.GetAsync<List<Product>>(cacheAllKeys);

        if (cachedProducts is not null)
            return cachedProducts;

        await _cacheLock.WaitAsync();

        try
        {
            cachedProducts = await cache.GetAsync<List<Product>>(cacheAllKeys);

            if (cachedProducts is not null)
                return cachedProducts;

            var products = await repository.GetAllAsync();

            await cache.SetAsync(cacheAllKeys, products);

            return products;
        }
        finally
        {
            _cacheLock.Release();
        }
    }

    public async Task<Product> CreateAsync(Product product)
    {
        await _cacheLock.WaitAsync();
        try
        {
            await repository.AddAsync(product);

            await cache.RemoveAsync(cacheAllKeys);

            return product;
        }
        finally
        {
            _cacheLock.Release();
        }

    }

    public async Task UpdateAsync(Product product)
    {
        var productLock = GetProductLock(product.Id);

        await productLock.WaitAsync();

        try
        {
            await _cacheLock.WaitAsync();
            try
            {
                var message = new OutboxMessage
                {
                    Type = "ProductUpdated",
                    Payload = JsonSerializer.Serialize(new
                    {
                        ProductId = product.Id
                    })
                };

                await repository.UpdateWithOutboxAsync(product, message);
            }
            finally
            {
                _cacheLock.Release();
            }
        }
        finally
        {
            productLock.Release();
        }
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var productLock = GetProductLock(id);

        await productLock.WaitAsync();
        try
        {
            await _cacheLock.WaitAsync();
            try
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
            finally
            {
                _cacheLock.Release();
            }
        }
        finally
        {
            productLock.Release();
        }
    }

}
