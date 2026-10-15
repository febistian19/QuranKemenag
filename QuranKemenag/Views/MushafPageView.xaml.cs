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
        // TampilkanLabelGable: true kalau bingkai ini perlu label nama surah (kiri) & info Juz (kanan)
        // di-overlay dari code, false kalau teksnya sudah baked-in di gambar (Fatihah & Baqarah).
        private record KonfigBingkai(string PathGambar, Thickness Margin, double LebarScrollViewer,
                                      double TinggiBaris, double UkuranFont, bool TampilkanLabelGable);

        private static readonly KonfigBingkai BingkaiGenerik =
            new("/Resources/Images/iluminasi.png", new Thickness(120, 195, 145, 65), 375, 42, 23, true);

        private static readonly KonfigBingkai BingkaiFatihah =
            new("/Resources/Images/iluminasi-fatihah.png", new Thickness(129, 175, 119, 168), 392, 76, 26, false);

        private static readonly KonfigBingkai BingkaiBaqarah =
            new("/Resources/Images/iluminasi-baqarah.png", new Thickness(119, 161, 113, 167), 408, 75, 25, false);

        // ===== Perkiraan kapasitas 1 halaman (jumlah karakter teks Arab) =====
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

        private static List<List<Ayat>> BagiJadiHalaman(int nomorSurah, List<Ayat> daftarAyat)
        {
            var hasil = new List<List<Ayat>>();
            var halamanBerjalan = new List<Ayat>();
            int panjangBerjalan = 0;

            foreach (var ayat in daftarAyat)
            {
                int indeksHalamanCalon = hasil.Count;
                int kapasitas = AmbilKapasitas(nomorSurah, indeksHalamanCalon);

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
            TeksMushaf.Width = konfig.LebarScrollViewer - 30;
            TeksMushaf.LineHeight = konfig.TinggiBaris;
            TeksMushaf.FontSize = konfig.UkuranFont;

            // ===== Label gable: nama surah (kiri) & info Juz (kanan) =====
            // Cuma ditampilkan untuk bingkai generik; Fatihah & Baqarah sudah punya teks baked-in di gambarnya.
            if (konfig.TampilkanLabelGable)
            {
                TxtNamaSurahArab.Text = _surah.NamaArab;

                int juzHalamanIni = _halaman[indeks].Count > 0 ? _halaman[indeks][0].Juz : 0;
                TxtInfoBawah.Text = juzHalamanIni > 0 ? $"الجزء {KeAngkaArab(juzHalamanIni)}" : "";

                TxtNamaSurahArab.Visibility = Visibility.Visible;
                TxtInfoBawah.Visibility = Visibility.Visible;
            }
            else
            {
                TxtNamaSurahArab.Visibility = Visibility.Collapsed;
                TxtInfoBawah.Visibility = Visibility.Collapsed;
            }

            ParagrafMushaf.Inlines.Clear();
            foreach (var ayat in _halaman[indeks])
            {
                if (ayat.NomorAyat == 0)
                {
                    ParagrafMushaf.Inlines.Add(new Run { Text = ayat.TeksArab });
                    continue;
                }

                var runAyat = new Run { Text = ayat.TeksArab + " " };
                if (ayat.KodeFont > 0) runAyat.FontFamily = ayat.FontArab;
                ParagrafMushaf.Inlines.Add(runAyat);
                ParagrafMushaf.Inlines.Add(new InlineUIContainer { Child = BuatPenandaAyat(ayat.NomorAyat, konfig.UkuranFont) });
                ParagrafMushaf.Inlines.Add(new Run { Text = " " });
            }

            TxtNomorHalaman.Text = (indeks + 1).ToString();
            BtnHalSebelumnya.IsEnabled = indeks > 0;
            BtnHalSelanjutnya.IsEnabled = indeks < _halaman.Count - 1;
        }

        private void BtnSebelumnya_Click(object sender, RoutedEventArgs e) => TampilkanHalaman(_halamanSaatIni - 1);

        private void BtnSelanjutnya_Click(object sender, RoutedEventArgs e) => TampilkanHalaman(_halamanSaatIni + 1);

        private static Border BuatPenandaAyat(int nomorAyat, double ukuranFontHalaman)
        {
            double diameter = Math.Clamp(ukuranFontHalaman * 1.45, 20, 34);
            var warnaHijau = (Brush)Application.Current.Resources["PrimaryGreenDarkBrush"];

            var teks = new TextBlock
            {
                Text = KeAngkaArab(nomorAyat),
                FontSize = Math.Max(ukuranFontHalaman * 0.62, 11),
                FontFamily = new FontFamily("LPMQ ISEP MISBAH"),
                Foreground = warnaHijau,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };

            return new Border
            {
                Width = diameter,
                Height = diameter,
                CornerRadius = new CornerRadius(diameter / 2),
                BorderBrush = warnaHijau,
                BorderThickness = new Thickness(1.2),
                Background = new SolidColorBrush(Microsoft.UI.Colors.Transparent),
                Margin = new Thickness(2, 0, 2, 0),
                Child = teks
            };
        }

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