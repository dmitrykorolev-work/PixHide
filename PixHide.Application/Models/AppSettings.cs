using CommunityToolkit.Mvvm.ComponentModel;

using PixHide.Application.Enums.Settings;

namespace PixHide.Application.Models;

public sealed partial class AppSettings : ObservableObject
{
    [ObservableProperty]
    public partial Theme Theme { get; set; } = Theme.System;

    [ObservableProperty]
    public partial BackdropType BackdropType { get; set; } = BackdropType.Mica;

    [ObservableProperty]
    public partial Language Language { get; set; } = Language.English;
}
