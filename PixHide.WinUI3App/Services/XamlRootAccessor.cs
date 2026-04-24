using Microsoft.UI.Xaml;
using PixHide.WinUI3App.Abstractions.Services;

namespace PixHide.WinUI3App.Services;

public sealed class XamlRootAccessor : IXamlRootAccessor
{
    public XamlRoot? Current { get; private set; }

    public void Set(XamlRoot xamlRoot) => Current = xamlRoot;

    public void Clear(XamlRoot xamlRoot)
    {
        if (ReferenceEquals(Current, xamlRoot))
            Current = null;
    }
}