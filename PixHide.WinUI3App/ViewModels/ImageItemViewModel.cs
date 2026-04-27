using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using PixHide.Application.Abstractions.Persistence;
using PixHide.Application.Abstractions.Services;
using PixHide.Application.DTOs;
using PixHide.WinUI3App.Abstractions.Services;
using PixHide.WinUI3App.Services;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Windows.Networking;
using Windows.Storage.Streams;

namespace PixHide.WinUI3App.ViewModels;

public partial class ImageItemViewModel : ObservableObject
{
    public static readonly double VariedImageSizeLayoutWidth = 150;
    public static readonly double ThumbnailWidth = VariedImageSizeLayoutWidth - 10;

    private readonly IImageItemService _imageItemService;

    public Guid Id { get; }

    public ImageItemDTO Model { get; private set; }
    private readonly IImageStorage _imageStorage;

    public ImageItemViewModel(ImageItemDTO model, IImageItemService imageItemService, IImageStorage imageStorage)
    {
        _imageItemService = imageItemService ?? throw new ArgumentNullException(nameof(imageItemService));
        _imageStorage = imageStorage ?? throw new ArgumentNullException(nameof(imageStorage));

        Model = model ?? throw new ArgumentNullException(nameof(model));

        
        Name = Model.Name;
        Id = Model.Id;
    }


    [ObservableProperty]
    public partial string? Name { get; set; }

    [ObservableProperty]
    public partial ImageSource? ImageSource { get; set; }

    [ObservableProperty]
    public partial double AspectRatio { get; set; } = 1;

    [ObservableProperty]
    public partial double ThumbnailHeight { get; set; }


    partial void OnNameChanged(string? value)
    {
        if (Model.Name == value) return;

        try
        {
            _imageItemService.SetNameAsync(Id, value ?? "");
            Model.Name = value ?? "";
        } catch (Exception ex) {
            Debug.WriteLine($"Failed to update name for {Name} ({Id}), Error: {ex.Message}");

            Name = Model.Name; // Revert to the old name on failure
        }
    }

    public async Task<bool> LoadAsync()
    {
        if (ImageSource is not null) return false;

        Stream? stream = null;

        try
        {
            Debug.WriteLine($"Loading image: {Name} ({Id})");

            stream = _imageStorage.Get( Id.ToString() );

            if (stream is null) return false;

            var bitmapImage = new BitmapImage();

            ImageSource = bitmapImage;

            bitmapImage.DecodePixelWidth = (int)VariedImageSizeLayoutWidth * 2; // Load a higher resolution image to ensure good quality when displayed at high DPI

            // Keep references to streams until the image is fully opened or failed
            var randomAccessStream = stream.AsRandomAccessStream();

            bitmapImage.ImageOpened += (s, e) =>
            {
                try
                {
                    var bmp = (BitmapImage)s;
                    double width = bmp.PixelWidth;
                    double height = bmp.PixelHeight;

                    AspectRatio = height / width;
                    ThumbnailHeight = ThumbnailWidth * AspectRatio;

                    Debug.WriteLine($"Loaded image: {Name} ({Id})");
                }
                finally
                {
                    // Dispose streams when image finished opening
                    try { randomAccessStream?.Dispose(); } catch { }
                    try { stream?.Dispose(); } catch { }
                }
            };

            bitmapImage.ImageFailed += (s, e) =>
            {
                try
                {
                    Debug.WriteLine($"Failed to load image: {Name} ({Id}), Error: {e.ErrorMessage}");
                }
                finally
                {
                    // Ensure streams are disposed on failure
                    try { randomAccessStream?.Dispose(); } catch { }
                    try { stream?.Dispose(); } catch { }
                }
            };

            await bitmapImage.SetSourceAsync(randomAccessStream);

            stream = null;
            randomAccessStream = null;

            return true;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Failed to load image {Name} ({Id}), Error: {ex.Message}");

            return false;
        }
        finally
        {
            stream?.Dispose();

            stream = null;
        }
    }

    public void Unload()
    {
        if (ImageSource is BitmapImage bmp)
        {
            try
            {
                bmp.UriSource = null;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to clear BitmapImage UriSource for {Name}, Error: {ex.Message}");
            }
        }

        ImageSource = null;
    }
}
