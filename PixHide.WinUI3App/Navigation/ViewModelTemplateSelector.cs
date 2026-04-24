using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using PixHide.WinUI3App.ViewModels;

namespace PixHide.WinUI3App.Navigation;
public sealed partial class ViewModelTemplateSelector : DataTemplateSelector
{
    public DataTemplate? HomeTemplate { get; set; }
    public DataTemplate? SettingsTemplate { get; set; }

    protected override DataTemplate SelectTemplateCore(object item, DependencyObject container)
    {
        return item switch
        {
            HomeViewModel => HomeTemplate!,
            SettingsViewModel => SettingsTemplate!,
            _ => base.SelectTemplateCore(item, container)
        };
    }
}