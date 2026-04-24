using PixHide.Application.Models;

namespace PixHide.Application.Abstractions.Services;

public interface ISettingsService
{
    AppSettings Settings { get; }

    Task LoadAsync();
    Task SaveAsync();

    event EventHandler? SettingsChanged;
}
