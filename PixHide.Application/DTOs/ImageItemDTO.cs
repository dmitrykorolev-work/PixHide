namespace PixHide.Application.DTOs;

public record ImageItemDTO
{
    public Guid Id { get; init; }
    public string Name { get; set; } = null!;
}