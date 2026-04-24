using Microsoft.Extensions.DependencyInjection;

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

using Microsoft.UI.Xaml.Media.Imaging;

using System;

using System.Diagnostics;
using System.IO;

using Windows.ApplicationModel.DataTransfer;

using Windows.Storage;
using Windows.System;

using PixHide.WinUI3App.Abstractions.Services;

using PixHide.WinUI3App.ViewModels;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace PixHide.WinUI3App.Views;

public sealed partial class HomeView : UserControl
{
    private const double RightPanelImageWidth = 150;
    private const double PreviewImageMinWidth = 300;

    private readonly IXamlRootAccessor _xamlRootAccessor;

    public HomeViewModel ViewModel => (HomeViewModel)this.DataContext;

    public HomeView()
    {
        _xamlRootAccessor = App.Services.GetRequiredService<IXamlRootAccessor>();

        this.InitializeComponent();
    }
    public async void OnLoaded(object sender, RoutedEventArgs e)
    {

        _xamlRootAccessor.Set(XamlRoot);

        ViewModel.RightImages.CollectionChanged += (s, e) =>
        {
            
            if (e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Remove)
            {
                try
                {
                    if (e.OldStartingIndex >= 0 && e.OldStartingIndex < ViewModel.RightImages.Count && ViewModel.RightImages[e.OldStartingIndex] is ImageItemViewModel item)
                    {
                        DispatcherQueue.TryEnqueue(() => _ = item?.LoadAsync());
                    }
                }
                catch (Exception) { }
            }
            
        };

        await ViewModel.OnLoaded();
        
    }

    private void EncoderTabScroll_Loaded(object sender, RoutedEventArgs e) => UpdateScrollPadding(EncoderTabScroll, EncoderTabPanel);
    private void EncoderTabScroll_SizeChanged(object sender, SizeChangedEventArgs e) => UpdateScrollPadding(EncoderTabScroll, EncoderTabPanel);
    
    private void DecoderTabScroll_Loaded(object sender, RoutedEventArgs e) => UpdateScrollPadding(DecoderTabScroll, DecoderTabPanel);
    private void DecoderTabScroll_SizeChanged(object sender, SizeChangedEventArgs e) => UpdateScrollPadding(DecoderTabScroll, DecoderTabPanel);

    // Shared helper to update right-side panel padding when vertical scrollbar appears
    private static void UpdateScrollPadding(ScrollViewer scrollViewer, object panel)
    {
        if (scrollViewer == null || panel == null)
            return;

        // If vertical scrollbar is visible, add right padding to compensate for its width
        var padding = scrollViewer.ComputedVerticalScrollBarVisibility == Visibility.Visible
            ? new Thickness(0, 0, 8, 0)
            : new Thickness(0);

        // Set Padding property if present on the panel type
        var prop = panel.GetType().GetProperty("Padding");
        if (prop != null && prop.CanWrite)
        {
            prop.SetValue(panel, padding);
        }
    }

    private void SideContent_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        // Calculate width of side content based on current window/column sizes
        double newSizeWidth = e.NewSize.Width;

        double windowWidth = Root.ActualWidth;
        double leftPanelWidth = MainGrid.ColumnDefinitions[0].ActualWidth;
        double availableWidth = CalculateAvailableRightPanelWidth(windowWidth, leftPanelWidth);

        Debug.WriteLine($"MainWindow Size: window_width={newSizeWidth}");

        // Fit side content to discrete image-width increments
        SideContent.Width = (int)( Math.Min(newSizeWidth, availableWidth) / RightPanelImageWidth ) * RightPanelImageWidth;
    }

    private void Root_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        double windowWidth = e.NewSize.Width;
        Debug.WriteLine($"MainWindow_SizeChanged: window_width={windowWidth}");

        double leftPanelWidth = MainGrid.ColumnDefinitions[0].ActualWidth;
        double availableWidth = CalculateAvailableRightPanelWidth(windowWidth, leftPanelWidth);

        if (availableWidth < SideContent.Width)
        {
            Debug.WriteLine($"SideContent.Width = {availableWidth}");
            SideContent.Width = (int)(availableWidth / RightPanelImageWidth) * RightPanelImageWidth;
        }
    }

    // Helper that computes how much width is available for the right side content
    private double CalculateAvailableRightPanelWidth(double windowWidth, double leftPanelWidth)
    {
        return windowWidth - leftPanelWidth - RightPanelSizer.ActualWidth - PreviewImageMinWidth;
    }

    private async void IconImage_Loaded(object sender, RoutedEventArgs e)
    {
        var image = (Image)sender;

        // Load embedded application icon resource into the Image control
        var asm = typeof(App).Assembly;
        using var stream = asm.GetManifestResourceStream("PixHide.WinUI3App.Assets.PixHideIcon.ico");

        if (stream == null)
            return;

        var bmp = new BitmapImage();
        await bmp.SetSourceAsync(stream.AsRandomAccessStream());
        image.Source = bmp;
    }

    private void PreviewArea_DragOver(object sender, DragEventArgs e)
    {
        Debug.WriteLine($"DragOver: DataView contains StorageItems: {e.DataView.Contains(StandardDataFormats.StorageItems)}");

        try
        {
            if (e.DataView.Contains(StandardDataFormats.StorageItems))
            {
                e.AcceptedOperation = DataPackageOperation.Copy;
            }
            else
            {
                e.AcceptedOperation = DataPackageOperation.None;
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex.ToString());
        }
    }

    private async void PreviewArea_Drop(object sender, DragEventArgs e)
    {
        try
        {
            if ( !e.DataView.Contains( StandardDataFormats.StorageItems ) )
                return;

            var items = await e.DataView.GetStorageItemsAsync();
            if ( items == null || items.Count == 0 )
                return;

            if ( items[0] is StorageFile storageFile )
            {
                if ( !HomeViewModel.IsAllowedImageFileName( storageFile.Name ) )
                    return;

                await ViewModel.HandleDropAsync( new Uri( storageFile.Path ) );
            }

        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex.ToString());
        }
    }

    private void VariedImageSizeRepeater_ElementPrepared(ItemsRepeater sender, ItemsRepeaterElementPreparedEventArgs args)
    {
        try
        {
            if ( TryGetImageItem(sender, args.Element, out var item) && item is not null )
            {
                _ = item.LoadAsync();
            }
        } catch (Exception ex) { Debug.WriteLine(ex.ToString()); }
    }

    private void VariedImageSizeRepeater_ElementClearing(ItemsRepeater sender, ItemsRepeaterElementClearingEventArgs args)
    {
        try
        {
            if ( TryGetImageItem(sender, args.Element, out var item) && item is not null )
            {
                item.Unload();
            }
        } catch (Exception ex) { Debug.WriteLine(ex.ToString()); }
    }

    // Try to resolve the ViewModel item for an element from the ItemsRepeater
    private bool TryGetImageItem(ItemsRepeater sender, UIElement element, out ImageItemViewModel? item)
    {
        item = null;

        try
        {
            if (sender == null || element == null)
                return false;

            var index = sender.GetElementIndex(element);

            var obj = sender.ItemsSourceView.GetAt(index);
            if (obj is ImageItemViewModel im)
            {
                item = im;
                return true;
            }
        } catch { }

        return false;
    }
}
