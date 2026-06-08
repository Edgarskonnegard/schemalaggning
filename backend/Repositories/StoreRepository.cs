using Microsoft.EntityFrameworkCore;
using Schemalaggning.Data;
using Schemalaggning.Models;
using Schemalaggning.Repositories.Interfaces;

namespace Schemalaggning.Repositories;

public class StoreRepository : IStoreRepository
{
    private readonly AppDbContext _context;

    public StoreRepository(AppDbContext context)
    {
        _context = context;
    }

    public Task<List<Store>> GetAllAsync()
    {
        return _context.Stores.AsNoTracking().OrderBy(store => store.Name).ToListAsync();
    }

    public Task<Store?> GetByIdAsync(int id)
    {
        return _context.Stores.FindAsync(id).AsTask();
    }

    public async Task<Store> CreateAsync(Store store)
    {
        _context.Stores.Add(store);
        await _context.SaveChangesAsync();
        return store;
    }

    public async Task<bool> UpdateAsync(Store store)
    {
        _context.Stores.Update(store);
        return await _context.SaveChangesAsync() > 0;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var store = await _context.Stores.FindAsync(id);
        if (store is null)
        {
            return false;
        }

        _context.Stores.Remove(store);
        return await _context.SaveChangesAsync() > 0;
    }

    public Task<bool> ExistsAsync(int id)
    {
        return _context.Stores.AnyAsync(store => store.Id == id);
    }
}
