using System;

using System.Threading.Tasks;

using Windows.Storage;
using Windows.Storage.Pickers;

using System.IO;

using PixHide.WinUI3App.Models;
using PixHide.WinUI3App.Abstractions.Services;

namespace PixHide.WinUI3App.Services;

internal class FileSaveService(WindowState windowState) : IFileSaveService
{
    public async Task<StorageFile?> SaveFileAsync(byte[] data, string suggestedFileName, string fileExtension, string fileTypeDescription)
    {
        var windowId = windowState.WindowId
           ?? throw new InvalidOperationException("WindowId is not initialized.");

        // Convert WindowId.Value (ulong) to native int (nint) expected by Initialize
        nint hwnd = (nint)windowId.Value;

        var picker = new FileSavePicker();
        WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);

        fileExtension = fileExtension.Trim();
        fileExtension = fileExtension.StartsWith(".") ? fileExtension : "." + fileExtension;

        picker.FileTypeChoices.Add( fileTypeDescription, [fileExtension] );

        picker.DefaultFileExtension = fileExtension;

        if ( !string.IsNullOrWhiteSpace( suggestedFileName ) )
            picker.SuggestedFileName = suggestedFileName;

        //picker.CommitButtonText = "Save File";

        // Show the picker dialog
        var result = await picker.PickSaveFileAsync();

        if (result != null)
        {
            string savePath = result.Path;
            await File.WriteAllBytesAsync( savePath, data );
        }

        return result;
    }
}
