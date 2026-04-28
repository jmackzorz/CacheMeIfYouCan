using CacheMeIfYouCan.Application.Repositories;
using CacheMeIfYouCan.Domain.Entities;
using CacheMeIfYouCan.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CacheMeIfYouCan.Infrastructure.Repositories;

public class ProductRepository : IProductRepository
{
    private readonly ApplicationDbContext _db;

    public ProductRepository(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task AddAsync(Product product)
    {
        await _db.Products.AddAsync(product);
        await _db.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        var entity = await _db.Products.FindAsync(id);
        if (entity is null) return;
        _db.Products.Remove(entity);
        await _db.SaveChangesAsync();
    }

    public async Task<IEnumerable<Product>> GetAllAsync()
    {
        return await _db.Products.Include(p => p.Category).ToListAsync();
    }

    public async Task<Product?> GetByIdAsync(int id)
    {
        return await _db.Products.Include(p => p.Category).FirstOrDefaultAsync(p => p.Id == id);
    }

    public async Task UpdateAsync(Product product)
    {
        _db.Products.Update(product);
        await _db.SaveChangesAsync();
    }
}
