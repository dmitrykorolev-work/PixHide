using Microsoft.UI.Xaml;

namespace PixHide.WinUI3App.Abstractions.Services;

public interface IXamlRootAccessor // Actually used just for showing ContentDialog's
{
    XamlRoot? Current { get; }
    void Set(XamlRoot xamlRoot);
    void Clear(XamlRoot xamlRoot);
}
