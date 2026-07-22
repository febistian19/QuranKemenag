using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using Windows.UI;

namespace QuranKemenag.Models
{
    public class Ayat
    {
        public int NomorSurah { get; set; }
        public int NomorAyat { get; set; }
        public string TeksArab { get; set; } = string.Empty;
        public string TeksLatin { get; set; } = string.Empty;
        public string Terjemah { get; set; } = string.Empty;
        public string? CatatanKaki { get; set; }

        //WarnaBookmark
        public Brush WarnaBookmark { get; set; } = new SolidColorBrush(Color.FromArgb(255, 211, 211, 211)); // setara LightGray

        // Properti bantu untuk binding Visibility di XAML
        public Visibility HasCatatanKaki => string.IsNullOrEmpty(CatatanKaki) ? Visibility.Collapsed : Visibility.Visible;
    }

    public class Juz
    {
        public int Nomor { get; set; }
        public string Rentang { get; set; } = string.Empty;
        public int SurahMulai { get; set; }
        public int AyatMulai { get; set; }
        public int SurahSelesai { get; set; }
        public int AyatSelesai { get; set; }
    }
}