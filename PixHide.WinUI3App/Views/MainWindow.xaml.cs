using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI;
using Microsoft.UI.Composition.SystemBackdrops;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

using Microsoft.UI.Xaml.Media;

using PixHide.Application.Abstractions.Services;
using PixHide.Application.Enums.Settings;

using PixHide.WinUI3App.ViewModels;
using System;

using System.Diagnostics;


// Theme and backdrop code adapted from https://github.com/microsoft/WinUI-Gallery/blob/main/WinUIGallery/Samples/SamplePages/SampleBuiltInSystemBackdropsWindow.xaml.cs

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace PixHide.WinUI3App.Views;

public sealed partial class MainWindow : Window
{
    private readonly ISettingsService _settingsService;

    private const int MIN_WIDTH = 900;
    private const int MIN_HEIGHT = 500;

    BackdropType currentBackdrop;

    public MainViewModel MainViewModel { get; } = App.Services.GetRequiredService<MainViewModel>();

    public MainWindow( ISettingsService settingsService )
    {
        _settingsService = settingsService;

        InitializeComponent();

        if ( settingsService.Settings.Theme != Theme.System)
            ((FrameworkElement)Content).RequestedTheme = _settingsService.Settings.Theme == Theme.Light ? ElementTheme.Light : ElementTheme.Dark;

        SetBackdrop( _settingsService.Settings.BackdropType );

        _settingsService.SettingsChanged += OnSettingsChanged;

        AppWindow.TitleBar.PreferredTheme = TitleBarTheme.UseDefaultAppMode;
        SetIconFromApplicationIcon(AppWindow);

        OverlappedPresenter presenter = OverlappedPresenter.Create();

        presenter.PreferredMinimumWidth = MIN_WIDTH;
        presenter.PreferredMinimumHeight = MIN_HEIGHT;

        AppWindow.SetPresenter(presenter);

        this.ExtendsContentIntoTitleBar = true;

        AppWindow.TitleBar.ButtonHoverBackgroundColor = AppWindow.TitleBar.ButtonBackgroundColor;
        AppWindow.TitleBar.ButtonPressedBackgroundColor = AppWindow.TitleBar.ButtonBackgroundColor;

        AppWindow.TitleBar.ButtonHoverForegroundColor = ColorHelper.FromArgb(255, 128, 128, 128);
        AppWindow.TitleBar.ButtonPressedForegroundColor = ColorHelper.FromArgb(255, 128, 128, 128);
    }

    public void OnSettingsChanged(object? sender, EventArgs e)
    {
        Debug.WriteLine("Settings changed, updating theme and backdrop.");

        if ( _settingsService.Settings.Theme != Theme.System )
            ((FrameworkElement)Content).RequestedTheme = _settingsService.Settings.Theme == Theme.Light ? ElementTheme.Light : ElementTheme.Dark;
        else
            ((FrameworkElement)Content).RequestedTheme = ElementTheme.Default;

        SetBackdrop( _settingsService.Settings.BackdropType );
    }

    public void SetBackdrop(BackdropType type)
    {
        // Reset to default color. If the requested type is supported, we'll update to that.
        // Note: This sample completely removes any previous controller to reset to the default
        //       state. This is done so this sample can show what is expected to be the most
        //       common pattern of an app simply choosing one controller type which it sets at
        //       startup. If an app wants to toggle between Mica and Acrylic it could simply
        //       call RemoveSystemBackdropTarget() on the old controller and then setup the new
        //       controller, reusing any existing configurationSource and Activated/Closed
        //       event handlers.

        //Reset the backdrop
        currentBackdrop = BackdropType.None;
        SystemBackdrop = null;

        //Set the backdrop
        if (type == BackdropType.Mica)
        {
            if (TrySetMicaBackdrop(false))
                currentBackdrop = type;
            else
            {
                // Mica isn't supported. Try Acrylic.
                type = BackdropType.Acrylic;

                Debug.WriteLine("Mica isn't supported. Trying Acrylic.");
            }
        }
        if (type == BackdropType.MicaAlt)
        {
            if (TrySetMicaBackdrop(true))
                currentBackdrop = type;
            else
            {
                // MicaAlt isn't supported. Try Acrylic.
                type = BackdropType.Acrylic;
                Debug.WriteLine("MicaAlt isn't supported. Trying Acrylic.");
            }
        }
        if (type == BackdropType.Acrylic)
        {
            if (TrySetAcrylicBackdrop())
                currentBackdrop = type;
            else
            {
                // Acrylic isn't supported, so take the next option, which is DefaultColor, which is already set.
                Debug.WriteLine("Acrylic isn't supported. Defaulting to no backdrop.");
            }
        }

        //Fix the none backdrop
        SetNoneBackdropBackground();
    }

    bool TrySetMicaBackdrop(bool useMicaAlt)
    {
        if (MicaController.IsSupported())
        {
            SystemBackdrop = new MicaBackdrop { Kind = useMicaAlt ? MicaKind.BaseAlt : MicaKind.Base }; ;
            return true;
        }

        return false; // Mica is not supported on this system
    }

    bool TrySetAcrylicBackdrop()
    {
        if (DesktopAcrylicController.IsSupported())
        {
            SystemBackdrop = new DesktopAcrylicBackdrop();
            return true;
        }

        return false; // Acrylic is not supported on this system
    }

    //Fixes the background color not changing when switching between themes.
    void SetNoneBackdropBackground()
    {
        if ( currentBackdrop == BackdropType.None && _settingsService.Settings.Theme != Theme.System )
            ((Grid)Content).Background = new SolidColorBrush(_settingsService.Settings.Theme == Theme.Light ? Colors.White : Colors.Black );
        else
            ((Grid)Content).Background = new SolidColorBrush(Colors.Transparent);
    }

    public static void SetIconFromApplicationIcon( AppWindow window )
    {   
        try
        {
            // https://learn.microsoft.com/en-us/answers/questions/822928/app-icon-windows-app-sdk.html
            string? sExe = Environment.ProcessPath;
            if (sExe == null) return;

            var ico = System.Drawing.Icon.ExtractAssociatedIcon(sExe);
            if (ico == null) return;

            window.SetIcon( Win32Interop.GetIconIdFromIcon( ico.Handle ) );
        }
        catch (Exception e)
        {
            Debug.WriteLine( e.ToString() );
        }
    }
}
