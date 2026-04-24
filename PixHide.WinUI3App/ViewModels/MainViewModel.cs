using CommunityToolkit.Mvvm.ComponentModel;

using PixHide.WinUI3App.Abstractions.Navigation;


namespace PixHide.WinUI3App.ViewModels;

//From https://kostyl.dev/csharp/desktop-ui/navigation-windows-part2
public partial class MainViewModel : ObservableObject
{
    private readonly INavigationStore _navigationStore;

    // CurrentViewModel тепер делегує до NavigationStore
    public ObservableObject? CurrentViewModel => _navigationStore.CurrentViewModel;

    public MainViewModel(INavigationStore navigationStore)
    {
        _navigationStore = navigationStore;
        // Підписуємось на зміни у NavigationStore
        _navigationStore.CurrentViewModelChanged += OnCurrentViewModelChanged;
    }

    private void OnCurrentViewModelChanged()
    {
        // Повідомляємо ContentControl що CurrentViewModel змінився
        OnPropertyChanged(nameof(CurrentViewModel));
    }
}