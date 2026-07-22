using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Documents;
using Microsoft.UI.Xaml.Media;
using QuranKemenag.Models;
using System.Collections.Generic;
using System.Text;

namespace QuranKemenag.Views
{
    public sealed partial class MushafPageView : UserControl
    {
        public MushafPageView(Surah surah, List<Ayat> daftarAyat)
        {
            this.InitializeComponent();

            TxtNamaSurahArab.Text = $"سُورَةُ {surah.NamaArab}";
            TxtInfoBawah.Text = daftarAyat.Count > 0 && daftarAyat[0].NomorAyat > 0
                ? $"{surah.NamaLatin} • Ayat {daftarAyat[0].NomorAyat}–{daftarAyat[^1].NomorAyat} dari {surah.JumlahAyat} ayat"
                : surah.NamaLatin;

            ParagrafMushaf.Inlines.Clear();

            foreach (var ayat in daftarAyat)
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
        }

        /// <summary>
        /// Membuat penanda akhir ayat bergaya mushaf: lingkaran emas berisi nomor ayat.
        /// </summary>
        private static Border BuatPenandaAyat(int nomorAyat)
        {
            var teks = new TextBlock
            {
                Text = KeAngkaArab(nomorAyat),
                FontSize = 13,
                FontFamily = new FontFamily("LPMQ ISEP MISBAH"),
                Foreground = (Brush)Application.Current.Resources["PrimaryGreenDarkBrush"],
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };

            return new Border
            {
                Width = 26,
                Height = 26,
                CornerRadius = new CornerRadius(13),
                Background = (Brush)Application.Current.Resources["GoldBrush"],
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