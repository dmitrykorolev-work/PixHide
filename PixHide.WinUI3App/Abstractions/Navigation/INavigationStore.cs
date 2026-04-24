using CommunityToolkit.Mvvm.ComponentModel;
using System;


namespace PixHide.WinUI3App.Abstractions.Navigation;

public interface INavigationStore
{
    ObservableObject? CurrentViewModel { get; set; }
    event Action? CurrentViewModelChanged;

    void NavigateToHome();
    void NavigateToSettings();
}
