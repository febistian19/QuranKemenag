using Microsoft.UI.Xaml.Media;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace QuranKemenag.Services
{
    /// <summary>
    /// Memetakan nomor font (kolom kode_font di msi_simbol.db) ke file Lpmq_Hafs_NN-Regular.ttf.
    /// Tiap ayat bisa memakai font berbeda: kode_font 1 -> Lpmq_Hafs_01, 30 -> Lpmq_Hafs_30, dst.
    /// </summary>
    public static class FontUtsmaniService
    {
        private static readonly Dictionary<int, FontFamily> _cache = new();

        public static string NamaFile(int kodeFont) => $"Lpmq_Hafs_{kodeFont:D2}-Regular.ttf";

        public static string BuatUri(int kodeFont) =>
            $"ms-appx:///Assets/Fonts/{NamaFile(kodeFont)}#Lpmq_Hafs_{kodeFont:D2}";

        public static FontFamily Dapatkan(int kodeFont)
        {
            if (!_cache.TryGetValue(kodeFont, out var font))
            {
                var path = Path.Combine(Windows.ApplicationModel.Package.Current.InstalledLocation.Path,
                                        "Assets", "Fonts", NamaFile(kodeFont));
                if (!File.Exists(path))
                    Debug.WriteLine($"[FontUtsmani] File font belum ada: {NamaFile(kodeFont)} (akan tampil kotak-kotak)");

                font = new FontFamily(BuatUri(kodeFont));
                _cache[kodeFont] = font;
            }
            return font;
        }
    }
}