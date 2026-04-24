namespace PixHide.Application.Abstractions.Persistence;

public interface IImageStorage
{
    Task SaveAsync(string filename, byte[] imageData);
    bool Delete(string filename);
    FileStream Get(string filename);

    string Extension { get; }
}