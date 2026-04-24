using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

using System.Diagnostics;

using PixHide.WinUI3App.Abstractions.Services;

using PixHide.WinUI3App.ViewModels;

namespace PixHide.WinUI3App.Views
{
    public sealed partial class SettingsView : UserControl
    {
        private readonly IXamlRootAccessor _xamlRootAccessor;

        public SettingsViewModel ViewModel => (SettingsViewModel)this.DataContext;

        public SettingsView()
        {
            _xamlRootAccessor = App.Services.GetRequiredService<IXamlRootAccessor>();

            InitializeComponent();
        }

        public void OnLoaded(object sender, RoutedEventArgs e)
        {
            Debug.WriteLine($"SettingsView loaded. XamlRoot: {XamlRoot}");
            _xamlRootAccessor.Set(XamlRoot);
        }
    }
}
