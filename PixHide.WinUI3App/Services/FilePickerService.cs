using System;
using System.Threading.Tasks;

using Windows.Storage.Pickers;
using Windows.Storage;
using System.Collections.Generic;

using PixHide.WinUI3App.Models;
using PixHide.WinUI3App.Abstractions.Services;

namespace PixHide.WinUI3App.Services;

internal class FilePickerService(WindowState windowState) : IFilePickerService
{
    public async Task<StorageFile?> PickOneFileAsync( HashSet<string>? allowedExtnsions = null )
    {
        var windowId = windowState.WindowId
            ?? throw new InvalidOperationException("WindowId is not initialized.");

        // Convert WindowId.Value (ulong) to native int (nint) expected by Initialize
        nint hwnd = (nint)windowId.Value;

        var picker = new FileOpenPicker();
        WinRT.Interop.InitializeWithWindow.Initialize( picker, hwnd );

        picker.CommitButtonText = "Pick File";
        picker.SuggestedStartLocation = PickerLocationId.PicturesLibrary;
        picker.ViewMode = PickerViewMode.List;

        // Add each allowed extension individually
        if ( allowedExtnsions != null )
        {
            foreach ( var ext in allowedExtnsions )
            {
                if ( !string.IsNullOrWhiteSpace( ext ) )
                    picker.FileTypeFilter.Add( ext );
            }
        } else
        {
            // If no specific extensions provided, allow all files
            picker.FileTypeFilter.Add( "*" );
        }

        StorageFile? file = await picker.PickSingleFileAsync();
        return file;
    }
}
