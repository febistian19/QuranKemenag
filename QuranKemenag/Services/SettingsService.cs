using System;
using Windows.Storage;
using QuranKemenag.Models;

namespace QuranKemenag.Services
{
    /// <summary>
    /// Simple settings service that persists application preferences to
    /// ApplicationData.Current.LocalSettings. This service is intentionally
    /// small and focused: it provides accessors for the Quran text mode
    /// (Unicode or Symbol) used by the application.
    /// </summary>
    public sealed class SettingsService
    {
        private const string KeyQuranTextMode = "CurrentQuranTextMode";

        private readonly ApplicationDataContainer _localSettings;

        /// <summary>
        /// Initializes a new instance of <see cref="SettingsService"/>.
        /// Uses ApplicationData.Current.LocalSettings as the storage backend.
        /// </summary>
        public SettingsService()
        {
            _localSettings = ApplicationData.Current.LocalSettings;
        }

        /// <summary>
        /// Gets or sets the currently selected <see cref="QuranTextMode"/>.
        /// The value is persisted in LocalSettings using a constant key.
        /// If no value is found in settings, the default value <see cref="QuranTextMode.Unicode"/>
        /// is returned.
        /// </summary>
        public QuranTextMode CurrentQuranTextMode
        {
            get
            {
                try
                {
                    if (_localSettings.Values.TryGetValue(KeyQuranTextMode, out var raw) && raw is string s)
                    {
                        if (Enum.TryParse<QuranTextMode>(s, ignoreCase: true, out var parsed))
                            return parsed;
                    }
                }
                catch
                {
                    // If LocalSettings is inaccessible or the content is malformed,
                    // fall back to the safe default.
                }

                return QuranTextMode.Unicode;
            }
            set
            {
                // Persist as string so migration or future enum changes remain readable.
                _localSettings.Values[KeyQuranTextMode] = value.ToString();
            }
        }

        /// <summary>
        /// Removes the persisted value and resets the setting to its default.
        /// </summary>
        public void ResetQuranTextModeToDefault()
        {
            if (_localSettings.Values.ContainsKey(KeyQuranTextMode))
            {
                _localSettings.Values.Remove(KeyQuranTextMode);
            }
        }
    }
}
