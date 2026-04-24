using System.Diagnostics;

using PixHide.Domain.Entities;

using PixHide.Application.Abstractions.Persistence;
using PixHide.Application.Abstractions.Services;
using PixHide.Application.DTOs;
using PixHide.Application.Mappings;

namespace PixHide.Application.Services;

public class ImageItemService(IImageItemRepository imageItemRepository, IImageStorage imageStorage, AppMapper mapper) : IImageItemService
{
    private readonly IImageItemRepository _imageItemRepository = imageItemRepository;
    private readonly IImageStorage _imageStorage = imageStorage;
    private readonly AppMapper _mapper = mapper;

    public async Task< ImageItemDTO > CreateAsync(CreateImageItemDTO dto)
    {
        var item = new ImageItem
        {
            Name = dto.Name
        };

        Debug.WriteLine($"Creating ImageItem {item.Name}");

        do item.Id = Guid.NewGuid(); // Ensure unique ID, though collisions are extremely unlikely
        while (await _imageItemRepository.GetByIdAsync(item.Id).ConfigureAwait(false) is not null);

        await _imageItemRepository.AddAsync(item).ConfigureAwait(false);
        await _imageStorage.SaveAsync(item.Id.ToString(), dto.ImageData).ConfigureAwait(false);

        return _mapper.ImageItemToImageItemDTO(item);
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        if (id == Guid.Empty) return false;
        if ( await _imageItemRepository.GetByIdAsync(id).ConfigureAwait(false) is null ) return false;

        Debug.WriteLine($"Deleting ImageItem {id}");

        _imageStorage.Delete(id.ToString());
        await _imageItemRepository.DeleteAsync(id).ConfigureAwait(false);

        return true;
    }

    public async Task< ImageItemDTO? > GetByIdAsync(Guid id)
    {
        var item = await _imageItemRepository.GetByIdAsync(id).ConfigureAwait(false);
        return item != null ? _mapper.ImageItemToImageItemDTO(item) : null;
    }

    public async Task< IEnumerable<ImageItemDTO> > GetAllAsync()
    {   
        var items = await _imageItemRepository.GetAllAsync().ConfigureAwait(false);
        return _mapper.ImageItemsToImageItemDTOs(items);
    }

    public async Task SetNameAsync(Guid id, string name)
    {
        var item = await _imageItemRepository.GetByIdAsync(id).ConfigureAwait(false);
        if (item != null && item.Name != name)
        {
            Debug.WriteLine($"Updating name of ImageItem {item.Id} from '{item.Name}' to '{name}'");

            item.Name = name;
            await _imageItemRepository.UpdateAsync(item).ConfigureAwait(false);
        }
    }
}
