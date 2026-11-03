using RedisCachingLab.Core.Entities;
using RedisCachingLab.Core.Interfaces;

namespace RedisCachingLab.Infrastructure.Services;

public class ProductService(IProductRepository repository, ICacheService cache) : IProductService
{
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

    public Task<List<Product>> GetAllAsync()
        => repository.GetAllAsync();

    public async Task<Product> CreateAsync(Product product)
    {
        await repository.AddAsync(product);

        return product;
    }


    public async Task UpdateAsync(Product product)
    {
        await repository.UpdateAsync(product);

    }

    public async Task DeleteAsync(int id)
    {
        var product = await repository.GetByIdAsync(id);

        if (product is null)
            return;

        await repository.DeleteAsync(product);
    }

}
