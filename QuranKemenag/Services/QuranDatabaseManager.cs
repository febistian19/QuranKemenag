using System;
using QuranKemenag.Models;

namespace QuranKemenag.Services
{
    /// <summary>
    /// Responsible for selecting the active Quran database asset based on application settings.
    /// This class does not perform any IO or database operations; it only maps the
    /// selected <see cref="QuranTextMode"/> to the corresponding asset path.
    /// </summary>
    public sealed class QuranDatabaseManager
    {
        /// <summary>
        /// Name of the assets folder relative path returned by this manager.
        /// </summary>
        public const string AssetsFolder = "Assets";

        /// <summary>
        /// File name of the Unicode database asset.
        /// Matches the existing project asset: Assets/quran.db
        /// </summary>
        public const string UnicodeDatabaseFileName = "quran.db";

        /// <summary>
        /// File name of the Symbol database asset.
        /// Matches the existing project asset: Assets/msi_simbol.db
        /// </summary>
        public const string SymbolDatabaseFileName = "msi_simbol.db";

        private readonly SettingsService _settingsService;

        /// <summary>
        /// Initializes a new instance of <see cref="QuranDatabaseManager"/> using a
        /// new <see cref="SettingsService"/> instance.
        /// </summary>
        public QuranDatabaseManager() : this(new SettingsService()) { }

        /// <summary>
        /// Initializes a new instance of <see cref="QuranDatabaseManager"/> with
        /// the provided <paramref name="settingsService"/>. Supplying the service
        /// makes the manager easier to test and reuse.
        /// </summary>
        /// <param name="settingsService">Settings service used to read current mode.</param>
        public QuranDatabaseManager(SettingsService settingsService)
        {
            _settingsService = settingsService ?? throw new ArgumentNullException(nameof(settingsService));
        }

        /// <summary>
        /// Gets the relative path to the currently active database asset.
        /// Examples: "Assets/quran.db" or "Assets/msi_simbol.db".
        /// If the stored mode is unknown, defaults to the Unicode asset.
        /// </summary>
        public string CurrentDatabasePath
        {
            get
            {
                var mode = _settingsService.CurrentQuranTextMode;

                return mode switch
                {
                    QuranTextMode.Symbol => GetAssetPath(SymbolDatabaseFileName),
                    _ => GetAssetPath(UnicodeDatabaseFileName),
                };
            }
        }

        /// <summary>
        /// Returns true when the current mode is Unicode.
        /// </summary>
        public bool IsUnicodeMode()
        {
            return _settingsService.CurrentQuranTextMode == QuranTextMode.Unicode;
        }

        /// <summary>
        /// Returns true when the current mode is Symbol.
        /// </summary>
        public bool IsSymbolMode()
        {
            return _settingsService.CurrentQuranTextMode == QuranTextMode.Symbol;
        }

        /// <summary>
        /// Builds the relative asset path for a given database file name.
        /// The method uses forward slashes which align with pack/asset URIs used in the app.
        /// </summary>
        /// <param name="fileName">Database file name (e.g. "quran.db").</param>
        /// <returns>Relative asset path such as "Assets/quran.db".</returns>
        private static string GetAssetPath(string fileName)
        {
            return $"{AssetsFolder}/{fileName}";
        }
    }
}
