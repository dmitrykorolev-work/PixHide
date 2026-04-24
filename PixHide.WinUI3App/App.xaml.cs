using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI;
using Microsoft.UI.Xaml;

using System;

using System.Diagnostics;
using System.IO;

using WinRT.Interop;

using PixHide.Application.Abstractions.Persistence;
using PixHide.Application.Abstractions.Services;
using PixHide.Application.Mappings;
using PixHide.Application.Services;
using PixHide.Infrastructure.Persistence;
using PixHide.Infrastructure.Repositories;
using PixHide.WinUI3App.Abstractions.Navigation;
using PixHide.WinUI3App.Abstractions.Services;
using PixHide.WinUI3App.Models;
using PixHide.WinUI3App.Navigation;
using PixHide.WinUI3App.Services;
using PixHide.WinUI3App.ViewModels;
using PixHide.WinUI3App.Views;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace PixHide.WinUI3App;

/// <summary>
/// Provides application-specific behavior to supplement the default Application class.
/// </summary>
public partial class App : Microsoft.UI.Xaml.Application
{
    public static IServiceProvider Services { get; private set; } = null!;

    private Window? _window;

    /// <summary>
    /// Initializes the singleton application object.  This is the first line of authored code
    /// executed, and as such is the logical equivalent of main() or WinMain().
    /// </summary>
    public App()
    {
        InitializeComponent();

        var services = new ServiceCollection();

        services.AddSingleton<AppMapper>();

        services.AddSingleton<WindowState>();

        services.AddSingleton<IXamlRootAccessor, XamlRootAccessor>();

        services.AddScoped<IFilePickerService, FilePickerService>();
        services.AddScoped<IFileSaveService, FileSaveService>();

        services.AddScoped<IDialogService, DialogService>();

        services.AddSingleton<INavigationStore, NavigationStore>();

        services.AddSingleton<MainViewModel>();
        services.AddTransient<HomeViewModel>();
        services.AddTransient<SettingsViewModel>();

        services.AddScoped<ILSBEncoderService, LSBEncoderService>();
        services.AddScoped<ILSBDecoderService, LSBDecoderService>();

        services.AddScoped<IImageItemRepository, SQLiteImageItemRepository>();

        services.AddScoped<IImageItemService, ImageItemService>();

        string appData = Environment.GetFolderPath( Environment.SpecialFolder.ApplicationData );
        string appDirectory = Path.Combine(appData, "PixHide");

        if (!string.IsNullOrEmpty(appDirectory) && !Directory.Exists(appDirectory))
        {
            Directory.CreateDirectory(appDirectory);
        }

        // Register DbContext
        services.AddDbContextFactory<PixHideDbContext>(options =>
            options.UseSqlite( $"Data Source={ Path.Combine(appDirectory, "pixhide.db") }" )
        );

        // Register Image Storage

        services.AddScoped<IImageStorage>(
            _ => new FileSystemImageStorage(
                Path.Combine(appDirectory, "images"),
                ".png"
            )
        );

        services.AddSingleton<ISettingsService>(
            _ => new JsonSettingsService(
                Path.Combine(appDirectory, "settings.json")
            )
        );

        services.AddSingleton<ILanguageService, LanguageService>();

        Services = services.BuildServiceProvider();

        var languageService = Services.GetRequiredService<ILanguageService>();
        languageService.Initialize();

        var navigationStore = Services.GetRequiredService<INavigationStore>();
        navigationStore.NavigateToHome();

        UnhandledException += (sender, e) => {
            Debug.WriteLine($"UnhandledException: {e.Exception}\n{e.Message}");
        };
    }

    /// <summary>
    /// Invoked when the application is launched.
    /// </summary>
    /// <param name="args">Details about the launch request and process.</param>
    protected override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
    {
        _window = new MainWindow( Services.GetRequiredService<ISettingsService>() );

        var hwnd = WindowNative.GetWindowHandle(_window);
        var windowId = Win32Interop.GetWindowIdFromWindow(hwnd);

        Services.GetRequiredService<WindowState>().WindowId = windowId;

        _window.Activate();
    }
}
