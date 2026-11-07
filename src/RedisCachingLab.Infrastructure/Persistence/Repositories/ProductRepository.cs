
using Microsoft.EntityFrameworkCore;
using RedisCachingLab.Core.Entities;
using RedisCachingLab.Core.Interfaces;

namespace RedisCachingLab.Infrastructure.Persistence.Repositories;

public class ProductRepository(AppDbContext context) : IProductRepository
{
    public async Task<Product?> GetByIdAsync(int id)
    {
        return await context.Products
            .FirstOrDefaultAsync(p => p.Id == id);
    }

    public async Task<List<Product>> GetAllAsync()
    {
        return await context.Products
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task AddAsync(Product product)
    {
        await context.Products.AddAsync(product);
        await context.SaveChangesAsync();
    }

    public async Task UpdateAsync(Product product)
    {
        var existingProduct = await context.Products
            .FindAsync(product.Id);

        if (existingProduct is null)
            throw new InvalidOperationException("Product not found.");

        context.Entry(existingProduct)
            .CurrentValues
            .SetValues(product);

        await context.SaveChangesAsync();
    }

    public async Task DeleteAsync(Product product)
    {
        context.Products.Remove(product);
        await context.SaveChangesAsync();
    }

    public async Task<bool> ExistsAsync(int id)
    {
        return await context.Products
            .AnyAsync(p => p.Id == id);
    }
}
