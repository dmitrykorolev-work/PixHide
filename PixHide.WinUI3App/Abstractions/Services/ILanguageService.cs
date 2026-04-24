using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using PixHide.Application.Enums.Settings;

namespace PixHide.WinUI3App.Abstractions.Services;

public interface ILanguageService
{
    Language CurrentLanguage { get; }

    IReadOnlyList<Language> SupportedLanguages { get; } // Including "System"

    void Initialize();

    Task SetLanguageAsync(Language language);

    event EventHandler? LanguageChanged;
}
