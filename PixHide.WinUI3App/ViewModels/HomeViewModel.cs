using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;

using OpenCvSharp;

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

using Windows.Storage.FileProperties;

using Microsoft.Windows.ApplicationModel.Resources;

using PixHide.Application.Abstractions.Persistence;
using PixHide.Application.Abstractions.Services;
using PixHide.Application.DTOs;

using PixHide.WinUI3App.Abstractions.Services;
using PixHide.WinUI3App.Enums;
using PixHide.WinUI3App.Helpers;

using PixHide.WinUI3App.Abstractions.Navigation;

namespace PixHide.WinUI3App.ViewModels;

// TODO: Separate Encoder and Decoder VM's

public sealed partial class HomeViewModel(
    IFilePickerService filePickerService,
    IFileSaveService fileSaveService,
    IDialogService dialogService,
    ILSBEncoderService encoderService,
    ILSBDecoderService decoderService,
    IImageItemService imageItemService,
    IImageStorage imageStorage,
    INavigationStore navigationStore
) : ObservableObject
{
    private readonly ResourceLoader _resourceLoader = new ();

    public async Task OnLoaded()
    {
        if (RightImages.Count > 0) return;

        var items = await imageItemService.GetAllAsync();

        foreach (var item in items)
        {
            RightImages.Insert(0, new ImageItemViewModel(item, imageItemService, imageStorage));
        }
    }


    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".png", ".jpg", ".jpeg", ".bmp", ".tiff"
    };

    public static bool IsAllowedImageFileName(string? fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
            return false;

        var ext = Path.GetExtension(fileName);
        return !string.IsNullOrEmpty(ext) && AllowedExtensions.Contains(ext);
    }

    [ObservableProperty]
    public partial ObservableCollection<ImageItemViewModel> RightImages { get; private set; } = new();

    public bool PreviewImageIsNull => PreviewImage == null; // Use converter?

    [NotifyPropertyChangedFor(nameof(PreviewImageIsNull))]
    [ObservableProperty]
    public partial ImageSource? PreviewImage { get; private set; }

    // =========================
    // Capacity properties
    // =========================

    // Encoder

    [NotifyPropertyChangedFor(nameof(EncoderCapacityPercent))]
    [NotifyPropertyChangedFor(nameof(EncoderCapacityOverflow))]
    [ObservableProperty]
    public partial ulong EncoderUsedCapacity { get; private set; } = 0;

    [NotifyPropertyChangedFor(nameof(EncoderCapacityPercent))]
    [NotifyPropertyChangedFor(nameof(EncoderCapacityOverflow))]
    [ObservableProperty]
    public partial ulong EncoderMaxCapacity { get; private set; } = 0;

    public double EncoderCapacityPercent =>
        EncoderMaxCapacity == 0 ? 0 : (EncoderUsedCapacity * 100.0 / EncoderMaxCapacity);

    public bool EncoderCapacityOverflow => EncoderMaxCapacity < EncoderUsedCapacity || EncoderMaxCapacity == 0;

    // Decoder

    [NotifyPropertyChangedFor(nameof(DecoderCapacityPercent))]
    [ObservableProperty]
    public partial ulong DecoderUsedCapacity { get; private set; } = 0;

    [NotifyPropertyChangedFor(nameof(DecoderCapacityPercent))]
    [ObservableProperty]
    public partial ulong DecoderMaxCapacity { get; private set; } = 0;

    public double DecoderCapacityPercent =>
        DecoderMaxCapacity == 0 ? 0 : (DecoderUsedCapacity * 100.0 / DecoderMaxCapacity);

    // =========================

    // Working status
    [NotifyCanExecuteChangedFor(nameof(OpenSettingsCommand))]
    [ObservableProperty]
    public partial bool EncoderWorking { get; private set; } = false;

    [ObservableProperty]
    public partial bool EncoderError { get; private set; } = false;

    [ObservableProperty]
    public partial bool EncoderPaused { get; private set; } = false;


    [NotifyCanExecuteChangedFor(nameof(OpenSettingsCommand))]
    [ObservableProperty]
    public partial bool DecoderWorking { get; private set; } = false;

    [ObservableProperty]
    public partial bool DecoderError { get; private set; } = false;

    [ObservableProperty]
    public partial bool DecoderPaused { get; private set; } = false;

    public async Task<bool> SetPreviewAsync(Uri uri)
    {
        try
        {
            PreviewImage = new BitmapImage(uri);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public async Task<bool> SetPreviewAsync(Mat mat)
    {
        try
        {
            var bitmap = await ImageConvert.MatToBitmapImageAsync(mat);
            PreviewImage = bitmap;
            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }

    // =========================
    // Mode switching (Encode / Decode)
    // =========================

    private AppMode mode = AppMode.Encode;

    public AppMode Mode
    {
        get => mode;
        set
        {
            if (SetProperty(ref mode, value))
            {
                OnPropertyChanged(nameof(Mode));
            }

            OnPropertyChanged(nameof(IsEncode)); // Allways notify these for update toogle buttons, even if mode value didn't change (e.g. user clicks already selected tab) to ensure UI stays in sync.
            OnPropertyChanged(nameof(IsDecode));
        }
    }

    public bool IsEncode => Mode == AppMode.Encode;
    public bool IsDecode => Mode == AppMode.Decode;

    [RelayCommand]
    private void SelectEncode()
    {
        Mode = AppMode.Encode;
    }

    [RelayCommand]
    private void SelectDecode()
    {
        Mode = AppMode.Decode;
    }


    [RelayCommand]
    public async Task PickImageFile()
    {
        var file = await filePickerService.PickOneFileAsync(AllowedExtensions);
        if (file == null) return;

        var uri = new Uri(file.Path);

        await SetWorkingImageAsync(uri);
    }

    public async Task HandleDropAsync(Uri uri)
    {
        await SetWorkingImageAsync(uri);
    }

    [RelayCommand]
    public async Task ImageItemOpen(ImageItemDTO imageItem)
    {
        var stream = imageStorage.Get(imageItem.Id.ToString());
        if (stream == null)
            return;

        var mat = Mat.FromStream(stream, ImreadModes.Color);

        stream?.Close();
        stream?.Dispose();

        await SetWorkingImageAsync(mat);
    }

    [RelayCommand]
    public async Task ImageItemSaveAs(ImageItemDTO imageItem)
    {
        var fileName = imageItem.Name.Split('.')[0];
        var stream = imageStorage.Get(imageItem.Id.ToString());
        if (stream == null) return;

        var fileBytes = new byte[stream.Length];

        await stream.ReadAsync(fileBytes, 0, fileBytes.Length);

        stream?.Close();
        stream?.Dispose();

        await fileSaveService.SaveFileAsync(fileBytes, fileName, imageStorage.Extension, "PNG Image");
    }

    [RelayCommand]
    public async Task ImageItemDelete(ImageItemDTO imageItem)
    {
        var confirmed = await dialogService.ConfirmAsync(
            _resourceLoader.GetString("ImageItemDeleteConfirmTitle"),
            _resourceLoader.GetString("ImageItemDeleteConfirmContent"),
            _resourceLoader.GetString("Yes"),
            _resourceLoader.GetString("No")
        );

        if (!confirmed) return;

        try
        {
            if (!await imageItemService.DeleteAsync(imageItem.Id)) return;

            var item = RightImages.FirstOrDefault(i => i.Id == imageItem.Id);
            if (item is not null) RightImages.Remove(item);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Failed to delete image item: {ex.Message}");
            await dialogService.ShowMessageAsync(_resourceLoader.GetString("DeleteImageFailed"), _resourceLoader.GetString("DeleteImageFailedMessage") + ex.Message);
        }
    }

    // Encoder and Decoder working Mats (not exposed to UI, used for processing in services)

    private Mat? _encoderWorkingMat;
    private Mat? _decoderWorkingMat;

    private async Task SetWorkingImageAsync(Uri uri)
    {
        _encoderWorkingMat = await ImageConvert.UriToMatAsync(uri);
        _decoderWorkingMat = _encoderWorkingMat.Clone(); // We need to clone the Mat because encoding operation will modify it, so we keep separate copies for each mode.

        await SetPreviewAsync(uri);

        EncodeCommand.NotifyCanExecuteChanged();
        DecodeCommand.NotifyCanExecuteChanged();

        UpdateEncoderCapacity();
    }

    private async Task SetWorkingImageAsync(Mat mat)
    {
        _encoderWorkingMat = mat;
        _decoderWorkingMat = mat.Clone();

        if (!await SetPreviewAsync(mat))
            Debug.WriteLine("Failed to set preview image from Mat.");

        EncodeCommand.NotifyCanExecuteChanged();
        DecodeCommand.NotifyCanExecuteChanged();

        UpdateEncoderCapacity();
    }

    [RelayCommand]
    public async Task WorkingImageSaveToList()
    {
        if ((_decoderWorkingMat is null) && (_encoderWorkingMat is null)) return;

        var fileBytes = (_decoderWorkingMat ?? _encoderWorkingMat)!.ToBytes(".png");

        await AddImageItemAsync("", fileBytes);
    }

    [RelayCommand]
    public async Task WorkingImageSaveAs()
    {
        if ((_decoderWorkingMat is null) && (_encoderWorkingMat is null)) return;

        var fileName = "image";
        var fileBytes = (_decoderWorkingMat ?? _encoderWorkingMat)!.ToBytes(".png");

        await fileSaveService.SaveFileAsync(fileBytes, fileName, imageStorage.Extension, "PNG Image");
    }

    // Pick data file for encoding

    private byte[]? _encoderInputFileData;

    [ObservableProperty]
    public partial string? EncoderInputFileName { get; private set; }
    partial void OnEncoderInputFileNameChanged(string? value) => UpdateEncoderCapacity();

    [RelayCommand]
    public async Task PickDataFile()
    {
        var file = await filePickerService.PickOneFileAsync(); // No filter for data files, user can pick any file
        if (file == null)
            return;

        BasicProperties properties = await file.GetBasicPropertiesAsync();

        EncoderUsedCapacity = properties.Size;

        _encoderInputFileData = await File.ReadAllBytesAsync(file.Path);
        EncoderInputFileName = file.Name;

        EncodeCommand.NotifyCanExecuteChanged();

        UpdateEncoderCapacity();
    }

    // Encoding parameters
    [ObservableProperty]
    public partial int EncoderBitsPerChannel { get; set; } = 1;
    partial void OnEncoderBitsPerChannelChanged(int value) => UpdateEncoderCapacity();

    [ObservableProperty]
    public partial bool EncoderDither { get; set; } = true;

    [ObservableProperty]
    public partial bool EncoderContrastCorrection { get; set; } = true;

    // Actions

    private bool CanEncode() =>
        _encoderWorkingMat != null &&
        _encoderInputFileData != null &&
        !string.IsNullOrEmpty(EncoderInputFileName) &&
        !EncoderCapacityOverflow &&
        EncoderBitsPerChannel > 0 && EncoderBitsPerChannel < 8;

    [RelayCommand(CanExecute = nameof(CanEncode))]
    public async Task Encode()
    {
        if (_encoderWorkingMat == null || _encoderInputFileData == null || EncoderInputFileName == null)
            return;

        EncoderWorking = true;

        try
        {
            var _encoderOutputMat = _encoderWorkingMat.Clone();

            await Task.Run(
                () => encoderService.EncodeToImage(
                    _encoderOutputMat,
                    _encoderInputFileData,
                    EncoderInputFileName,
                    EncoderBitsPerChannel,
                    EncoderDither,
                    EncoderContrastCorrection
                )
            );

            _decoderWorkingMat = _encoderOutputMat;

            await SetPreviewAsync(_encoderOutputMat);

            var imgBytes = _encoderOutputMat.ToBytes(".png");

            await AddImageItemAsync(EncoderInputFileName, imgBytes);

            EncoderPaused = true;

            await fileSaveService.SaveFileAsync(imgBytes, "encoded_image", ".png", "PNG Image");

            EncoderPaused = false;
        }
        catch (Exception ex)
        {
            EncoderError = true;

            Debug.WriteLine($"Encoding failed: {ex.Message}");
            await dialogService.ShowMessageAsync(_resourceLoader.GetString("EncodingFailed"), _resourceLoader.GetString("EncodingFailedMessage") + ex.Message);

            EncoderError = false;
        }

        EncoderWorking = false;

        GC.Collect(2); // Force GC to collect any unmanaged resources from OpenCV Mats that are no longer needed after encoding, to free up memory.
        GC.WaitForPendingFinalizers();
        GC.Collect(2);
    }

    private bool CanDecode() =>
        _decoderWorkingMat != null;

    [RelayCommand(CanExecute = nameof(CanDecode))]
    public async Task Decode()
    {
        if (_decoderWorkingMat == null)
            return;

        DecoderWorking = true;

        try
        {
            var result = await Task.Run(
                () => decoderService.DecodeFromImage(_decoderWorkingMat)
            );

            var decodedInfo = $"Decoded file: {result.FileName}, Size: {result.FileSize} bytes";

            Debug.WriteLine(decodedInfo);

            DecoderMaxCapacity = result.MaxCapacity;
            DecoderUsedCapacity = result.FileSize;

            DecoderPaused = true;

            await fileSaveService.SaveFileAsync(result.Data, result.FileName ?? "decoded_file", Path.GetExtension(result.FileName ?? "bin"), "All Files");

            DecoderPaused = false;

            result = null;
        }
        catch (InvalidDataException ex)
        {
            DecoderError = true;
            Debug.WriteLine($"Decoding failed: InvalidDataException ({ex.Message})");
            await dialogService.ShowMessageAsync(_resourceLoader.GetString("DecodingFailed"), _resourceLoader.GetString("NoHiddenDataFound"));
            DecoderError = false;
        }
        catch (Exception ex)
        {
            DecoderError = true;

            Debug.WriteLine($"Decoding failed: {ex.Message}");
            await dialogService.ShowMessageAsync(_resourceLoader.GetString("DecodingFailed"), _resourceLoader.GetString("DecodingFailedMessage") + ex.Message);

            DecoderError = false;
        }

        GC.Collect(2); // Force GC to collect any unmanaged resources from OpenCV Mats that are no longer needed after encoding, to free up memory.
        GC.WaitForPendingFinalizers();
        GC.Collect(2);

        DecoderWorking = false;
    }

    private void UpdateEncoderCapacity()
    {
        if (_encoderWorkingMat is null)
        {
            EncoderMaxCapacity = 0;
            return;
        }

        if (EncoderBitsPerChannel <= 0 || EncoderBitsPerChannel >= 8)
        {
            EncoderMaxCapacity = 0;
            return;
        }

        try
        {
            EncoderMaxCapacity = (ulong)encoderService.GetMaxDataBytes(
                _encoderWorkingMat,
                EncoderBitsPerChannel,
                EncoderInputFileName?.Length ?? 0);
        }
        catch
        {
            EncoderMaxCapacity = 0;
        }

        EncodeCommand.NotifyCanExecuteChanged();
    }

    public async Task AddImageItemAsync(string fileName, byte[] imgBytes)
    {
        try
        {
            var item = await imageItemService.CreateAsync(new CreateImageItemDTO(
                fileName,
                imgBytes
            ));

            RightImages.Insert(0, new ImageItemViewModel(item, imageItemService, imageStorage));
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Failed to add image item: {ex.Message}");
            await dialogService.ShowMessageAsync(_resourceLoader.GetString("AddImageFailed"), _resourceLoader.GetString("AddImageFailedMessage") + ex.Message);
        }
    }

    // OpenSettings Command
    private bool CanOpenSettings() =>
        !DecoderWorking && !EncoderWorking;

    [RelayCommand(CanExecute = nameof(CanOpenSettings))]
    public async Task OpenSettings()
    {
        navigationStore.NavigateToSettings();
    }
}