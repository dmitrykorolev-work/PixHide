using System.Collections.Generic;
using System.Threading.Tasks;
using Windows.Storage;

namespace PixHide.WinUI3App.Abstractions.Services;

public interface IFilePickerService
{
    Task<StorageFile?> PickOneFileAsync(HashSet<string>? allowedExtnsions = null);
}
