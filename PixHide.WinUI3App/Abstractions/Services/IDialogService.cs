using System.Threading.Tasks;

namespace PixHide.WinUI3App.Abstractions.Services;

public interface IDialogService
{
    Task<bool> ConfirmAsync(string title, string message, string primaryButtonText = "Yes", string secondaryButtonText = "No");
    Task ShowMessageAsync(string title, string message);
}
