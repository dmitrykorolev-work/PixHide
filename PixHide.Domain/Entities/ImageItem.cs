namespace PixHide.Domain.Entities;

public class ImageItem
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
}
