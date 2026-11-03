using RedisCachingLab.Core.Entities;

namespace RedisCachingLab.Core.Interfaces;

public interface IProductRepository
{
    Task<Product?> GetByIdAsync(int id);

    Task<List<Product>> GetAllAsync();

    Task AddAsync(Product product);

    Task UpdateAsync(Product product);

    Task DeleteAsync(Product product);

    Task<bool> ExistsAsync(int id);
}
