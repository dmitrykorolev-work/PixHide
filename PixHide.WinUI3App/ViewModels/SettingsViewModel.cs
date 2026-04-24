using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using Microsoft.Windows.ApplicationModel.Resources;

using System.Collections.ObjectModel;
using System.Diagnostics;

using System.Threading.Tasks;

using PixHide.Application.Abstractions.Services;

using PixHide.WinUI3App.Abstractions.Navigation;
using PixHide.WinUI3App.Abstractions.Services;

using PixHide.Application.Enums.Settings;


namespace PixHide.WinUI3App.ViewModels;

public sealed partial class SettingsViewModel : ObservableObject
{
    private readonly ResourceLoader _resourceLoader = new();

    private readonly INavigationStore _navigationStore;
    private readonly ISettingsService _settingsService;
    private readonly ILanguageService _languageService;
    private readonly IDialogService _dialogService;

    public SettingsViewModel( INavigationStore navigationStore, ISettingsService settingsService, ILanguageService languageService, IDialogService dialogService )
    {
        _navigationStore = navigationStore;
        _settingsService = settingsService;
        _languageService = languageService;
        _dialogService = dialogService;

        SelectedLanguage = _languageService.CurrentLanguage;
        SelectedBackdrop = settingsService.Settings.BackdropType;
        SelectedTheme = settingsService.Settings.Theme;
    }

    public ObservableCollection<Language> Languages { get; } = [Language.English, Language.Ukrainian];

    [ObservableProperty]
    public partial Language SelectedLanguage { get; set; } = Language.English;


    public ObservableCollection<BackdropType> BackdropTypes { get; } = [BackdropType.Mica, BackdropType.MicaAlt, BackdropType.Acrylic];

    [ObservableProperty]
    public partial BackdropType SelectedBackdrop { get; set; } = BackdropType.Mica;


    public ObservableCollection<Theme> Themes { get; } = [Theme.System, Theme.Dark, Theme.Light];

    [ObservableProperty]
    public partial Theme SelectedTheme { get; set; } = Theme.System;

    partial void OnSelectedLanguageChanged(Language oldValue, Language newValue)
    {
        if (newValue == _languageService.CurrentLanguage ) return;
        Debug.WriteLine($"SelectedLanguage changed from {oldValue} to {newValue}");

        _languageService.SetLanguageAsync(newValue);

        _dialogService.ShowMessageAsync(_resourceLoader.GetString("LanguageChangeTitle"), _resourceLoader.GetString("LanguageChangeMessage"));
    }

    partial void OnSelectedBackdropChanged(BackdropType oldValue, BackdropType newValue)
    {
        if (newValue == _settingsService.Settings.BackdropType) return;
        Debug.WriteLine($"SelectedBackdrop changed from {oldValue} to {newValue}");

        _settingsService.Settings.BackdropType = newValue;
        _settingsService.SaveAsync();
    }

    partial void OnSelectedThemeChanged(Theme oldValue, Theme newValue)
    {
        if (newValue == _settingsService.Settings.Theme) return;
        Debug.WriteLine($"SelectedTheme changed from {oldValue} to {newValue}");

        _settingsService.Settings.Theme = newValue;
        _settingsService.SaveAsync();
    }

    // GoBack Command

    [RelayCommand()]
    public async Task GoBack()
    {
        _navigationStore.NavigateToHome();
    }
}