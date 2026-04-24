using PixHide.Domain.Entities;

namespace PixHide.Application.Abstractions.Persistence;

public interface IImageItemRepository
{
    Task<ImageItem?> GetByIdAsync(Guid id);
    Task< IEnumerable<ImageItem> > GetAllAsync();
    Task AddAsync(ImageItem imageItem);
    Task UpdateAsync(ImageItem imageItem);
    Task<bool> DeleteAsync(Guid id);
}
