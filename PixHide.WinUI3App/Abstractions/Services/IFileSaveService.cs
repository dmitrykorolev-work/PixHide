using System.Threading.Tasks;
using Windows.Storage;

namespace PixHide.WinUI3App.Abstractions.Services;

public interface IFileSaveService
{
    Task<StorageFile?> SaveFileAsync(byte[] data, string suggestedFileName, string fileExtension, string fileTypeDescription);
}
