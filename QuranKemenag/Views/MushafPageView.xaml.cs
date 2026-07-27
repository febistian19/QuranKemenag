using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Documents;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using QuranKemenag.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace QuranKemenag.Views
{
    public sealed partial class MushafPageView : UserControl
    {
        // ===== Konfigurasi tiap jenis bingkai: path gambar + margin area putih (di kanvas 640x800) =====
        // Margin di bawah ini hasil deteksi presisi dari gambar aslinya, bukan tebakan.
        // Kalau nanti area putih di gambar diganti/direvisi, cukup ubah angka di sini.
        private record KonfigBingkai(string PathGambar, Thickness Margin, double LebarScrollViewer, double TinggiBaris, double UkuranFont);

        private static readonly KonfigBingkai BingkaiGenerik =
            new("/Resources/Images/iluminasi.png", new Thickness(120, 195, 145, 65), 375, 42, 23);

        // LineHeight dibuat jauh lebih lebar (76) khusus halaman ini, karena isinya selalu tetap
        // (7 ayat pendek) — supaya teksnya meregang mengisi penuh tinggi bingkai, bukan menumpuk di atas.
        private static readonly KonfigBingkai BingkaiFatihah =
            new("/Resources/Images/iluminasi-fatihah.png", new Thickness(129, 175, 119, 168), 392, 76, 26);

        private static readonly KonfigBingkai BingkaiBaqarah =
            new("/Resources/Images/iluminasi-baqarah.png", new Thickness(119, 161, 113, 167), 408, 75, 25);

        // ===== Perkiraan kapasitas 1 halaman (jumlah karakter teks Arab) =====
        // Belum ada data resmi pembagian 15-baris Kemenag, jadi ini estimasi berdasarkan
        // ukuran area putih & font. Kalau halaman terasa terlalu penuh/kosong, sesuaikan angka ini.
        private const int KapasitasGenerik = 600;
        private const int KapasitasFatihah = 530;
        private const int KapasitasBaqarah = 350;

        private readonly Surah _surah;
        private readonly List<List<Ayat>> _halaman;
        private int _halamanSaatIni = 0;

        public MushafPageView(Surah surah, List<Ayat> daftarAyat)
        {
            this.InitializeComponent();

            _surah = surah;
            _halaman = BagiJadiHalaman(surah.Nomor, daftarAyat);

            TampilkanHalaman(0);
        }

        /// <summary>
        /// Membagi daftar ayat jadi beberapa "halaman" berdasarkan perkiraan kapasitas bingkai.
        /// Halaman pertama Al-Fatihah & Al-Baqarah pakai bingkai & kapasitas khusus;
        /// selain itu (dan halaman ke-2 dst) pakai bingkai generik.
        /// </summary>
        private static List<List<Ayat>> BagiJadiHalaman(int nomorSurah, List<Ayat> daftarAyat)
        {
            var hasil = new List<List<Ayat>>();
            var halamanBerjalan = new List<Ayat>();
            int panjangBerjalan = 0;

            foreach (var ayat in daftarAyat)
            {
                int indeksHalamanCalon = hasil.Count;
                int kapasitas = AmbilKapasitas(nomorSurah, indeksHalamanCalon);

                // Baru pindah ke halaman baru kalau halaman SEKARANG sudah penuh/lewat kapasitas.
                // Jadi halaman selalu diisi SAMPAI penuh dulu, baru berpindah — walau akibatnya
                // kadang sedikit "meluber" karena 1 ayat panjang, itu lebih baik daripada
                // menyisakan banyak ruang kosong di halaman sebelumnya.
                if (halamanBerjalan.Count > 0 && panjangBerjalan >= kapasitas)
                {
                    hasil.Add(halamanBerjalan);
                    halamanBerjalan = new List<Ayat>();
                    panjangBerjalan = 0;
                }

                halamanBerjalan.Add(ayat);
                panjangBerjalan += ayat.TeksArab.Length;
            }

            if (halamanBerjalan.Count > 0) hasil.Add(halamanBerjalan);
            if (hasil.Count == 0) hasil.Add(new List<Ayat>());

            return hasil;
        }

        private static int AmbilKapasitas(int nomorSurah, int indeksHalaman)
        {
            if (indeksHalaman == 0 && nomorSurah == 1) return KapasitasFatihah;
            if (indeksHalaman == 0 && nomorSurah == 2) return KapasitasBaqarah;
            return KapasitasGenerik;
        }

        private static KonfigBingkai AmbilKonfigBingkai(int nomorSurah, int indeksHalaman)
        {
            if (indeksHalaman == 0 && nomorSurah == 1) return BingkaiFatihah;
            if (indeksHalaman == 0 && nomorSurah == 2) return BingkaiBaqarah;
            return BingkaiGenerik;
        }

        private void TampilkanHalaman(int indeks)
        {
            if (indeks < 0 || indeks >= _halaman.Count) return;
            _halamanSaatIni = indeks;

            var konfig = AmbilKonfigBingkai(_surah.Nomor, indeks);

            ImgBingkai.Source = new BitmapImage(new Uri("ms-appx://" + konfig.PathGambar));
            GridKontenAyat.Margin = konfig.Margin;
            SvTeksMushaf.Width = konfig.LebarScrollViewer;
            TeksMushaf.Width = konfig.LebarScrollViewer - 30; // dikurangi padding kiri+kanan ScrollViewer
            TeksMushaf.LineHeight = konfig.TinggiBaris;
            TeksMushaf.FontSize = konfig.UkuranFont;

            ParagrafMushaf.Inlines.Clear();
            foreach (var ayat in _halaman[indeks])
            {
                if (ayat.NomorAyat == 0)
                {
                    ParagrafMushaf.Inlines.Add(new Run { Text = ayat.TeksArab });
                    continue;
                }

                ParagrafMushaf.Inlines.Add(new Run { Text = ayat.TeksArab + " " });
                ParagrafMushaf.Inlines.Add(new InlineUIContainer { Child = BuatPenandaAyat(ayat.NomorAyat) });
                ParagrafMushaf.Inlines.Add(new Run { Text = "  " });
            }

            TxtNomorHalaman.Text = (indeks + 1).ToString();
            BtnHalSebelumnya.IsEnabled = indeks > 0;
            BtnHalSelanjutnya.IsEnabled = indeks < _halaman.Count - 1;
        }

        private void BtnSebelumnya_Click(object sender, RoutedEventArgs e) => TampilkanHalaman(_halamanSaatIni - 1);

        private void BtnSelanjutnya_Click(object sender, RoutedEventArgs e) => TampilkanHalaman(_halamanSaatIni + 1);

        /// <summary>
        /// Membuat penanda akhir ayat bergaya mushaf: lingkaran berisi nomor ayat Arab.
        /// </summary>
        private static Border BuatPenandaAyat(int nomorAyat)
        {
            var teks = new TextBlock
            {
                Text = KeAngkaArab(nomorAyat),
                FontSize = 22,
                FontFamily = new FontFamily("LPMQ ISEP MISBAH"),
                Foreground = (Brush)Application.Current.Resources["PrimaryGreenDarkBrush"],
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };

            return new Border
            {
                Width = 44,
                Height = 44,
                CornerRadius = new CornerRadius(13),
                Opacity = 0.9,
                Child = teks
            };
        }

        /// <summary>Mengonversi angka biasa ke angka Arab (٠١٢٣٤٥٦٧٨٩) untuk gaya mushaf.</summary>
        private static string KeAngkaArab(int angka)
        {
            string[] digitArab = { "٠", "١", "٢", "٣", "٤", "٥", "٦", "٧", "٨", "٩" };
            var teks = angka.ToString();
            var hasil = new StringBuilder();
            foreach (char c in teks) hasil.Append(digitArab[c - '0']);
            return hasil.ToString();
        }
    }
}