using PixHide.Application.DTOs;

namespace PixHide.Application.Abstractions.Services;

public interface IImageItemService
{
    Task<ImageItemDTO> CreateAsync(CreateImageItemDTO dto);

    Task<bool> DeleteAsync(Guid id);

    Task<ImageItemDTO?> GetByIdAsync(Guid id);

    Task< IEnumerable<ImageItemDTO> > GetAllAsync();

    Task SetNameAsync(Guid id, string name);
}
