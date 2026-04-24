namespace PixHide.Application.DTOs;

public record CreateImageItemDTO(
    string Name,
    byte[] ImageData
);