using PixHide.Application.Abstractions.Persistence;

namespace PixHide.Infrastructure.Persistence;

public class FileSystemImageStorage : IImageStorage
{
    private readonly string _basePath;
    private readonly string _extension;
    public string Extension => _extension;

    public FileSystemImageStorage(string basePath, string extension)
    {
        if ( string.IsNullOrWhiteSpace(basePath) )
            throw new ArgumentException("Base path is not set");

        if ( string.IsNullOrWhiteSpace(extension) )
            throw new ArgumentException("File extension is not set");

        _basePath = basePath;
        _extension = extension;

        Directory.CreateDirectory( _basePath );
    }

    public async Task SaveAsync(string filename, byte[] imageData)
    {
        if ( string.IsNullOrWhiteSpace(filename) ) throw new ArgumentException("Filename is not set");

        var path = GetFilePath(filename);
        await File.WriteAllBytesAsync(path, imageData);
    }

    public FileStream Get(string filename)
    {
        if ( string.IsNullOrWhiteSpace(filename) ) throw new ArgumentException("Filename is not set");

        var path = GetFilePath(filename);

        if ( !File.Exists(path) )
            throw new FileNotFoundException($"Image not found: {filename}");

        FileStream stream = File.OpenRead(path);
        return stream;
    }

    public bool Delete(string filename)
    {
        if ( string.IsNullOrWhiteSpace(filename) ) throw new ArgumentException("Filename is not set");
        var path = GetFilePath(filename);

        if ( File.Exists(path) )
        {
            File.Delete(path);
            return true;
        }

        return false;
    }

    private string GetFilePath(string filename)
    {
        if ( string.IsNullOrWhiteSpace(_extension) ) throw new InvalidOperationException("File extension is not set");

        filename += (_extension.StartsWith('.') ? _extension : "." + _extension);

        return Path.Combine(_basePath, filename );
    }
}
