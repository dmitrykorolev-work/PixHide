using OpenCvSharp;

using Microsoft.UI.Xaml.Media.Imaging;
using System;
using System.IO;

using System.Diagnostics;

using System.Threading.Tasks;
using Windows.Storage;

namespace PixHide.WinUI3App.Helpers;

public static class ImageConvert
{
    public static async Task<BitmapImage> MatToBitmapImageAsync( Mat mat, string ext = ".png" )
    {
        ArgumentNullException.ThrowIfNull(mat);

        byte[] bytes = mat.ToBytes( ext );

        var image = new BitmapImage();

        using var ms = new MemoryStream( bytes );
        await image.SetSourceAsync( ms.AsRandomAccessStream() );
        return image;
    }

    public static async Task<Mat> UriToMatAsync( Uri uri )
    {
        ArgumentNullException.ThrowIfNull(uri);

        StorageFile file = uri.Scheme.Equals( "ms-appx", StringComparison.OrdinalIgnoreCase )
            ? await StorageFile.GetFileFromApplicationUriAsync( uri )
            : await StorageFile.GetFileFromPathAsync( uri.LocalPath );

        byte[] bytes;

        using (var stream = await file.OpenStreamForReadAsync())
        using (var ms = new MemoryStream())
        {
            await stream.CopyToAsync( ms );
            bytes = ms.ToArray();
        }

        return Mat.FromImageData( bytes, ImreadModes.Color );
    }
}