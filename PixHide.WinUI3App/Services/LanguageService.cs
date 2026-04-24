using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

using PixHide.Application.Abstractions.Services;
using PixHide.WinUI3App.Abstractions.Services;
using PixHide.Application.Enums.Settings;

namespace PixHide.WinUI3App.Services
{
    public sealed class LanguageService(ISettingsService settingsService) : ILanguageService
    {
        private readonly ISettingsService _settingsService = settingsService ?? throw new ArgumentNullException(nameof(settingsService));

        private static readonly IReadOnlyList<Language> _supported =
            new ReadOnlyCollection<Language>( [Language.English, Language.Ukrainian, Language.System] );

        public Language CurrentLanguage { get; private set; } = Language.System;

        public IReadOnlyList<Language> SupportedLanguages => _supported;

        public event EventHandler? LanguageChanged;

        public void Initialize()
        {
            CurrentLanguage = _settingsService.Settings.Language;

            ApplyLanguageOverride(CurrentLanguage);
        }

        public async Task SetLanguageAsync(Language language)
        {
            if (language == CurrentLanguage)
                return;

            ApplyLanguageOverride(language);

            _settingsService.Settings.Language = language;
            await _settingsService.SaveAsync().ConfigureAwait(false);

            CurrentLanguage = language;

            LanguageChanged?.Invoke(this, EventArgs.Empty);
        }

        private static string ToLanguageTag(Language language)
        {
            return language switch
            {
                Language.English => "en-US",
                Language.Ukrainian => "uk-UA",
                Language.System => string.Empty,
                _ => string.Empty,
            };
        }

        private static void ApplyLanguageOverride(Language language)
        {
            var tag = ToLanguageTag(language);

            Windows.Globalization.ApplicationLanguages.PrimaryLanguageOverride = tag ?? string.Empty;

            //ResourceContext.GetForViewIndependentUse().Reset();
        }
    }
}
