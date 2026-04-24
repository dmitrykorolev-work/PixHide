using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

using System;
using System.Diagnostics;

using System.Threading.Tasks;

using PixHide.WinUI3App.Abstractions.Services;

namespace PixHide.WinUI3App.Services;

public sealed class DialogService(IXamlRootAccessor root) : IDialogService
{
    public async Task<bool> ConfirmAsync(string title, string message, string primaryButtonText = "Yes", string secondaryButtonText = "No")
    {
        Debug.WriteLine($"XamlRoot: {root.Current}");

        // https://github.com/microsoft/microsoft-ui-xaml/issues/2331
        var dialog = new ContentDialog
        {
            XamlRoot = root.Current ?? throw new InvalidOperationException("No XamlRoot."),
            Style = Microsoft.UI.Xaml.Application.Current.Resources["DefaultContentDialogStyle"] as Style,
            Title = title,
            Content = message,
            PrimaryButtonText = primaryButtonText,
            SecondaryButtonText = secondaryButtonText
        };

        return await dialog.ShowAsync() == ContentDialogResult.Primary;
    }

    public async Task ShowMessageAsync(string title, string message)
    {
        Debug.WriteLine($"XamlRoot: {root.Current}");

        var dialog = new ContentDialog
        {
            XamlRoot = root.Current ?? throw new InvalidOperationException("No XamlRoot."),
            Style = Microsoft.UI.Xaml.Application.Current.Resources["DefaultContentDialogStyle"] as Style,
            Title = title,
            Content = message,
            CloseButtonText = "Close"
        };

        await dialog.ShowAsync();
    }
}
