using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using Windows.UI;
using QuranKemenag.Services;
using QuranKemenag.Views;

namespace QuranKemenag.Models
{
    public class Ayat
    {
        public int NomorSurah { get; set; }
        public int NomorAyat { get; set; }
        public int Juz { get; set; }   // <-- TAMBAHKAN baris ini
        public string TeksArab { get; set; } = string.Empty;
        public string TeksLatin { get; set; } = string.Empty;
        public string Terjemah { get; set; } = string.Empty;
        public string? CatatanKaki { get; set; }

        // Nomor font Utsmani (kolom kode_font di msi_simbol.db). 0 = bukan mode Utsmani (pakai font pilihan biasa).
        public int KodeFont { get; set; } = 0;
        public int KodeFontBismillah { get; set; } = 0;

        // Font yang dipakai menampilkan teks Arab ayat ini: per-ayat kalau Utsmani, selain itu font pilihan pengguna.
        public FontFamily FontArab => KodeFont > 0
            ? FontUtsmaniService.Dapatkan(KodeFont)
            : AyatReaderView.SelectedFontFamily;

        public FontFamily FontBismillah => KodeFontBismillah > 0
            ? FontUtsmaniService.Dapatkan(KodeFontBismillah)
            : AyatReaderView.SelectedFontFamily;

        //WarnaBookmark
        public Brush WarnaBookmark { get; set; } = new SolidColorBrush(Color.FromArgb(255, 211, 211, 211)); // setara LightGray

        // Properti bantu untuk binding Visibility di XAML
        public Visibility HasCatatanKaki => string.IsNullOrEmpty(CatatanKaki) ? Visibility.Collapsed : Visibility.Visible;

        // ===== Dipakai khusus untuk tampilan "Per Juz" yang bisa menyambung lintas-surah =====
        // Entri "pembatas" ini bukan ayat sungguhan — cuma banner nama surah yang disisipkan
        // di antara ayat, muncul saat rentang juz pindah dari 1 surah ke surah berikutnya.
        public bool IsPembatasSurah { get; set; } = false;
        public string NamaSurahUntukPembatas { get; set; } = string.Empty;
        public string NamaSurahArabUntukPembatas { get; set; } = string.Empty;
        public string TempatTurunUntukPembatas { get; set; } = string.Empty;
        public string JumlahAyatArabUntukPembatas { get; set; } = string.Empty;
        public bool TampilkanBismillah { get; set; } = false;

        // mengambil basmalah dari database yang bersangkutan, untuk ditampilkan di atas ayat pertama surah (kecuali surah At-Taubah)
        public string TeksBismillah { get; set; } = string.Empty;

        public Visibility VisibilitasDivider => IsPembatasSurah ? Visibility.Visible : Visibility.Collapsed;
        public Visibility VisibilitasAyatBiasa => IsPembatasSurah ? Visibility.Collapsed : Visibility.Visible;
        public Visibility VisibilitasBismillah => TampilkanBismillah ? Visibility.Visible : Visibility.Collapsed;
    }

    public class Juz
    {
        public int Nomor { get; set; }
        public string Rentang { get; set; } = string.Empty;
        public int SurahMulai { get; set; }
        public int AyatMulai { get; set; }
        public int SurahSelesai { get; set; }
        public int AyatSelesai { get; set; }

        public string NomorTampil => $"Juz {Nomor}";
    }
}