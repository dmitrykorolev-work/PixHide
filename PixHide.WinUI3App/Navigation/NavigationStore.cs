using CommunityToolkit.Mvvm.ComponentModel;
using System;

using Microsoft.Extensions.DependencyInjection;

using PixHide.WinUI3App.Abstractions.Navigation;
using PixHide.WinUI3App.ViewModels;

namespace PixHide.WinUI3App.Navigation;

// Modified from https://kostyl.dev/csharp/desktop-ui/navigation-windows-part2
public class NavigationStore : INavigationStore
{
    private ObservableObject? _currentViewModel;

    public ObservableObject? CurrentViewModel
    {
        get => _currentViewModel;
        set
        {
            _currentViewModel = value;
            // Публікуємо подію — всі підписники дізнаються про зміну
            CurrentViewModelChanged?.Invoke();
        }
    }

    // Подія без аргументів — підписується MainViewModel
    public event Action? CurrentViewModelChanged;

    public void NavigateToHome()
    {
        CurrentViewModel = App.Services.GetRequiredService<HomeViewModel>();
    }

    public void NavigateToSettings()
    {
        CurrentViewModel = App.Services.GetRequiredService<SettingsViewModel>();
    }
}