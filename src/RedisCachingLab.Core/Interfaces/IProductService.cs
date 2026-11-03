using RedisCachingLab.Core.Entities;

namespace RedisCachingLab.Core.Interfaces;

public interface IProductService
{
    Task<Product?> GetByIdAsync(int id);

    Task<List<Product>> GetAllAsync();

    Task<Product> CreateAsync(Product product);

    Task UpdateAsync(Product product);

    Task<bool> DeleteAsync(int id);
}
