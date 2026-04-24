using Riok.Mapperly.Abstractions;

using PixHide.Application.DTOs;
using PixHide.Domain.Entities;

namespace PixHide.Application.Mappings
{
    [Mapper(AllowNullPropertyAssignment = false)]
    public partial class AppMapper
    {
        public partial ImageItemDTO ImageItemToImageItemDTO(ImageItem imageItem);

        public partial IEnumerable<ImageItemDTO> ImageItemsToImageItemDTOs(IEnumerable<ImageItem> imageItems);
    }
}