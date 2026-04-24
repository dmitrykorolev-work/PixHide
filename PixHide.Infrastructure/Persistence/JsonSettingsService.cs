using System.Text.Json;

using PixHide.Application.Models;
using PixHide.Application.Abstractions.Services;

namespace PixHide.Infrastructure.Persistence;

public sealed class JsonSettingsService : ISettingsService
{
    private readonly string _filePath;
    private readonly JsonSerializerOptions _jsonSerializerOptions = new() { WriteIndented = true };

    public AppSettings Settings { get; private set; } = new();

    public event EventHandler? SettingsChanged;

    public JsonSettingsService(string filePath)
    {
        ArgumentNullException.ThrowIfNull( nameof(filePath) );
        _filePath = filePath;

        if (!File.Exists(_filePath))
            return;

        var json = File.ReadAllText(_filePath);
        Settings = JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
    }

    public async Task LoadAsync()
    {
        if ( !File.Exists(_filePath) )
            return;

        var json = await File.ReadAllTextAsync(_filePath);
        Settings = JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
        SettingsChanged?.Invoke(this, EventArgs.Empty);
    }

    public async Task SaveAsync()
    {
        var json = JsonSerializer.Serialize(Settings, _jsonSerializerOptions);

        await File.WriteAllTextAsync(_filePath, json);
        SettingsChanged?.Invoke(this, EventArgs.Empty);
    }
}