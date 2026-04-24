using Microsoft.EntityFrameworkCore;
using PixHide.Application.Abstractions.Persistence;
using PixHide.Domain.Entities;
using PixHide.Infrastructure.Persistence;
using System.Diagnostics;

namespace PixHide.Infrastructure.Repositories;

public sealed class SQLiteImageItemRepository(PixHideDbContext context) : IImageItemRepository
{
    private readonly PixHideDbContext _context = context;

    public async Task<ImageItem?> GetByIdAsync(Guid id)
    {
        return await _context.ImageItems
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == id)
            .ConfigureAwait(false);
    }

    public async Task<IEnumerable<ImageItem>> GetAllAsync()
    {
        return await _context.ImageItems
            .AsNoTracking()
            .ToListAsync()
            .ConfigureAwait(false);
    }

    public async Task AddAsync(ImageItem ImageItem)
    {
        Debug.WriteLine($"Adding ImageItem: {ImageItem.Name}");

        await _context.ImageItems.AddAsync(ImageItem).ConfigureAwait(false);
        await _context.SaveChangesAsync().ConfigureAwait(false);
    }

    public async Task UpdateAsync(ImageItem ImageItem)
    {
        // Trying to find an entity that is already tracked or exists in the database
        var existing = await _context.ImageItems.FindAsync(ImageItem.Id).ConfigureAwait(false);
        if (existing is null)
        {
            // If there is no entity in the context / DB - attach it and mark it as Modified
            _context.ImageItems.Attach(ImageItem);
            _context.Entry(ImageItem).State = EntityState.Modified;
        }
        else
        {
            // If an entity is found / tracked - just update its values.
            _context.Entry(existing).CurrentValues.SetValues(ImageItem);
        }

        await _context.SaveChangesAsync().ConfigureAwait(false);
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var entity = await _context.ImageItems.FindAsync(id).ConfigureAwait(false);
        if (entity is null) return false;

        _context.ImageItems.Remove(entity);
        await _context.SaveChangesAsync().ConfigureAwait(false);

        Debug.WriteLine($"Deleted ImageItem with ID: {id}");

        return true;
    }
}