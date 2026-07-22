using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using QuranKemenag.Data.Database;
using QuranKemenag.Models;
using QuranKemenag.Services;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage;
using Windows.Storage.Pickers;

namespace QuranKemenag.Views
{
    public sealed partial class AyatReaderView : UserControl
    {
        private readonly MainWindow _mainWindow;
        private int _fontSize = 26;
        private List<Ayat> _ayatSaatIni = new();
        private Surah? _surahSaatIni;
        private List<Surah> _daftarSurah = new();

        // Dipakai lewat x:Bind statis supaya semua kartu di ItemsControl bisa "melihat" status
        // checkbox tampilan tanpa perlu ElementName (yang tidak menembus DataTemplate di WinUI).
        public static bool TampilkanAyatGlobal { get; private set; } = true;
        public static bool TampilkanLatinGlobal { get; private set; } = true;
        public static bool TampilkanTerjemahGlobal { get; private set; } = true;
        public static double UkuranFontGlobal { get; private set; } = 26;

        public AyatReaderView(MainWindow mainWindow, int nomorSurahAwal, int ayatMulai = 1)
        {
            InitializeComponent();
            _mainWindow = mainWindow;

            UkuranFontGlobal = _fontSize;

            Loaded += async (s, e) => await InisialisasiAsync(nomorSurahAwal, ayatMulai);
        }

        private async Task InisialisasiAsync(int nomorSurahAwal, int ayatMulai)
        {
            _daftarSurah = await QuranDatabase.AmbilDaftarSurahAsync();
            CmbSurah.ItemsSource = _daftarSurah;
            CmbSurah.SelectedValue = nomorSurahAwal;

            await MuatSurahAsync(nomorSurahAwal, ayatMulai);
        }

        private async Task MuatSurahAsync(int nomorSurah, int ayatMulai = 1)
        {
            _surahSaatIni = _daftarSurah.FirstOrDefault(s => s.Nomor == nomorSurah);
            if (_surahSaatIni == null) return;

            CmbAyatDari.ItemsSource = Enumerable.Range(1, _surahSaatIni.JumlahAyat).ToList();
            CmbAyatSampai.ItemsSource = Enumerable.Range(1, _surahSaatIni.JumlahAyat).ToList();

            CmbAyatDari.SelectedIndex = 0;
            CmbAyatSampai.SelectedIndex = _surahSaatIni.JumlahAyat - 1;

            await TampilkanAyatAsync(nomorSurah, 1, _surahSaatIni.JumlahAyat);

            // Ayat lain tetap ditampilkan semua; kalau ada ayat tujuan (mis. dari bookmark/juz),
            // geser scroll supaya ayat itu yang terlihat di paling atas.
            if (ayatMulai > 1)
            {
                ScrollKeAyat(ayatMulai);
            }
        }

        private void ScrollKeAyat(int nomorAyat)
        {
            var index = _ayatSaatIni.FindIndex(a => a.NomorAyat == nomorAyat);
            if (index < 0) return;

            // Tunggu satu siklus render supaya semua kartu ayat sudah selesai digambar,
            // baru hitung posisinya dan geser ScrollViewer ke sana.
            DispatcherQueue.TryEnqueue(Microsoft.UI.Dispatching.DispatcherQueuePriority.Low, () =>
            {
                PanelTeksArab.UpdateLayout();

                if (AyatItemsControl.ContainerFromIndex(index) is FrameworkElement container)
                {
                    var posisi = container.TransformToVisual(PanelTeksArab).TransformPoint(new Windows.Foundation.Point(0, 0));
                    double offsetTarget = PanelTeksArab.VerticalOffset + posisi.Y - 24; // 24px jarak dari atas
                    PanelTeksArab.ChangeView(null, Math.Max(0, offsetTarget), null, false);
                }
            });
        }

        private async Task TampilkanAyatAsync(int nomorSurah, int dari, int sampai)
        {
            var semuaAyatSurah = await QuranDatabase.AmbilAyatSurahAsync(nomorSurah);

            if (semuaAyatSurah.Count == 0)
            {
                _ayatSaatIni = new List<Ayat>
                {
                    new Ayat
                    {
                        NomorSurah = nomorSurah,
                        NomorAyat = 0,
                        TeksArab = "Ayat untuk surah ini belum tersedia di database",
                        TeksLatin = "",
                        Terjemah = "Pastikan berkas quran.db yang dipakai sudah berisi data surah ini."
                    }
                };
            }
            else
            {
                _ayatSaatIni = semuaAyatSurah.Where(a => a.NomorAyat >= dari && a.NomorAyat <= sampai).ToList();

                foreach (var ayat in _ayatSaatIni)
                {
                    ayat.WarnaBookmark = BookmarkService.SudahDisimpan(nomorSurah, ayat.NomorAyat)
                        ? new SolidColorBrush(Colors.Goldenrod)
                        : new SolidColorBrush(Colors.LightGray);
                }
            }

            AyatItemsControl.ItemsSource = null;
            AyatItemsControl.ItemsSource = _ayatSaatIni;

            RenderMushafJikaAktif();
        }

        private void RenderMushafJikaAktif()
        {
            if (CmbModeTampilan.SelectedIndex == 1 && _surahSaatIni != null)
            {
                PanelMushaf.Children.Clear();
                var mushafView = new MushafPageView(_surahSaatIni, _ayatSaatIni);
                PanelMushaf.Children.Add(mushafView);
            }
        }

        private void CmbModeTampilan_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Guard: event ini bisa terpicu saat InitializeComponent() masih berjalan,
            // sebelum semua elemen bernama selesai dibuat.
            if (PanelTeksArab == null || PanelMushaf == null || PanelUkuranFont == null ||
                PanelCentangPerAyat == null || PanelCentangPerHalaman == null || BtnSalinTerjemah == null) return;

            bool mushaf = CmbModeTampilan.SelectedIndex == 1;

            PanelTeksArab.Visibility = mushaf ? Visibility.Collapsed : Visibility.Visible;
            PanelMushaf.Visibility = mushaf ? Visibility.Visible : Visibility.Collapsed;
            PanelCentangPerAyat.Visibility = mushaf ? Visibility.Collapsed : Visibility.Visible;
            PanelCentangPerHalaman.Visibility = mushaf ? Visibility.Visible : Visibility.Collapsed;
            PanelUkuranFont.Visibility = mushaf ? Visibility.Collapsed : Visibility.Visible;
            BtnSalinTerjemah.Visibility = mushaf ? Visibility.Collapsed : Visibility.Visible;

            if (mushaf) RenderMushafJikaAktif();
        }

        private async void CmbSurah_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (CmbSurah.SelectedValue is int nomor)
            {
                await MuatSurahAsync(nomor);
            }
        }

        private async void Tampilkan_Click(object sender, RoutedEventArgs e)
        {
            if (CmbSurah.SelectedValue is int nomor &&
                CmbAyatDari.SelectedItem is int dari &&
                CmbAyatSampai.SelectedItem is int sampai)
            {
                await TampilkanAyatAsync(nomor, dari, sampai);
            }
        }

        private void Kembali_Click(object sender, RoutedEventArgs e)
        {
            _mainWindow.NavSurahDariHalamanLain();
        }

        private void FontTambah_Click(object sender, RoutedEventArgs e)
        {
            _fontSize = Math.Min(_fontSize + 2, 48);
            TxtUkuranFont.Text = _fontSize.ToString();
            UkuranFontGlobal = _fontSize;
            SegarkanKartuAyat();
        }

        private void FontKurang_Click(object sender, RoutedEventArgs e)
        {
            _fontSize = Math.Max(_fontSize - 2, 14);
            TxtUkuranFont.Text = _fontSize.ToString();
            UkuranFontGlobal = _fontSize;
            SegarkanKartuAyat();
        }

        private void ChkTampilan_Changed(object sender, RoutedEventArgs e)
        {
            // Guard: dipanggil juga saat InitializeComponent() sebelum semua checkbox dibuat.
            if (ChkAyat == null || ChkLatin == null || ChkTerjemah == null) return;

            TampilkanAyatGlobal = ChkAyat.IsChecked == true;
            TampilkanLatinGlobal = ChkLatin.IsChecked == true;
            TampilkanTerjemahGlobal = ChkTerjemah.IsChecked == true;
            SegarkanKartuAyat();
        }

        /// <summary>
        /// x:Bind static (UkuranFontGlobal, TampilkanAyatGlobal, dst) tidak otomatis memicu
        /// refresh tiap kartu di ItemsControl, jadi ItemsSource di-reset manual di sini.
        /// </summary>
        private void SegarkanKartuAyat()
        {
            if (AyatItemsControl == null) return;
            var data = _ayatSaatIni;
            AyatItemsControl.ItemsSource = null;
            AyatItemsControl.ItemsSource = data;
        }

        private void SalinKePapanKlip(string teks)
        {
            var paket = new DataPackage();
            paket.SetText(teks);
            Clipboard.SetContent(paket);
        }

        private void SalinAyat_Click(object sender, RoutedEventArgs e)
        {
            if (_ayatSaatIni.Count == 0) return;
            var sb = new StringBuilder();
            foreach (var a in _ayatSaatIni) sb.AppendLine(a.TeksArab);
            SalinKePapanKlip(sb.ToString());
        }

        private void SalinDenganTerjemah_Click(object sender, RoutedEventArgs e)
        {
            if (_ayatSaatIni.Count == 0) return;
            var sb = new StringBuilder();
            foreach (var a in _ayatSaatIni)
            {
                sb.AppendLine(a.TeksArab);
                sb.AppendLine(a.Terjemah);
                sb.AppendLine();
            }
            SalinKePapanKlip(sb.ToString());
        }

        private async Task<bool> AdaAyatUntukEksporAsync()
        {
            if (_ayatSaatIni.Count == 0 || _ayatSaatIni.All(a => a.NomorAyat == 0))
            {
                await TampilkanDialogAsync("Ekspor", "Tidak ada ayat yang bisa diekspor untuk surah/rentang ini.");
                return false;
            }
            return true;
        }

        private string JudulSurahUntukEkspor() => _surahSaatIni != null
            ? $"{_surahSaatIni.NamaLatin} ({_surahSaatIni.Arti}) - QS. {_surahSaatIni.Nomor}"
            : "Al-Quran";

        private async Task TampilkanDialogAsync(string judul, string pesan)
        {
            var dialog = new ContentDialog
            {
                Title = judul,
                Content = pesan,
                CloseButtonText = "OK",
                XamlRoot = this.XamlRoot
            };
            await dialog.ShowAsync();
        }

        private async void EksporWord_Click(object sender, TappedRoutedEventArgs e)
        {
            if (!await AdaAyatUntukEksporAsync()) return;

            var picker = new FileSavePicker();
            WinRT.Interop.InitializeWithWindow.Initialize(picker, WinRT.Interop.WindowNative.GetWindowHandle(_mainWindow));
            picker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;
            picker.FileTypeChoices.Add("Dokumen Word", new List<string> { ".docx" });
            picker.SuggestedFileName = _surahSaatIni?.NamaLatin ?? "Ayat";

            StorageFile? file = await picker.PickSaveFileAsync();
            if (file == null) return;

            try
            {
                WordExportService.EksporAyat(
                    filePath: file.Path,
                    judulSurah: JudulSurahUntukEkspor(),
                    daftarAyat: _ayatSaatIni,
                    tampilkanAyat: ChkAyat.IsChecked == true,
                    tampilkanLatin: ChkLatin.IsChecked == true,
                    tampilkanTerjemah: ChkTerjemah.IsChecked == true,
                    ukuranFontArab: _fontSize
                );

                await TawarkanBukaFileAsync(file.Path, "Word");
            }
            catch (Exception ex)
            {
                await TampilkanDialogAsync("Ekspor Word", $"Gagal mengekspor ke Word:\n{ex.Message}");
            }
        }

        private async void EksporPpt_Click(object sender, TappedRoutedEventArgs e)
        {
            if (!await AdaAyatUntukEksporAsync()) return;

            var picker = new FileSavePicker();
            WinRT.Interop.InitializeWithWindow.Initialize(picker, WinRT.Interop.WindowNative.GetWindowHandle(_mainWindow));
            picker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;
            picker.FileTypeChoices.Add("Presentasi PowerPoint", new List<string> { ".pptx" });
            picker.SuggestedFileName = _surahSaatIni?.NamaLatin ?? "Ayat";

            StorageFile? file = await picker.PickSaveFileAsync();
            if (file == null) return;

            try
            {
                PowerPointExportService.EksporAyat(
                    filePath: file.Path,
                    judulSurah: JudulSurahUntukEkspor(),
                    daftarAyat: _ayatSaatIni,
                    tampilkanLatin: ChkLatin.IsChecked == true,
                    tampilkanTerjemah: ChkTerjemah.IsChecked == true
                );

                await TawarkanBukaFileAsync(file.Path, "PowerPoint");
            }
            catch (Exception ex)
            {
                await TampilkanDialogAsync("Ekspor PowerPoint", $"Gagal mengekspor ke PowerPoint:\n{ex.Message}");
            }
        }

        private async Task TawarkanBukaFileAsync(string filePath, string namaFormat)
        {
            var dialog = new ContentDialog
            {
                Title = $"Ekspor {namaFormat}",
                Content = $"Berhasil diekspor ke {namaFormat}.\n\nBuka file sekarang?",
                PrimaryButtonText = "Buka",
                CloseButtonText = "Nanti saja",
                XamlRoot = this.XamlRoot
            };

            var hasil = await dialog.ShowAsync();
            if (hasil == ContentDialogResult.Primary)
            {
                Process.Start(new ProcessStartInfo(filePath) { UseShellExecute = true });
            }
        }

        private void BookmarkAyat_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is int nomorAyat && _surahSaatIni != null)
            {
                BookmarkService.Toggle(_surahSaatIni.Nomor, nomorAyat, _surahSaatIni.NamaLatin);

                var ayat = _ayatSaatIni.FirstOrDefault(a => a.NomorAyat == nomorAyat);
                if (ayat != null)
                {
                    ayat.WarnaBookmark = BookmarkService.SudahDisimpan(_surahSaatIni.Nomor, nomorAyat)
                        ? new SolidColorBrush(Colors.Goldenrod)
                        : new SolidColorBrush(Colors.LightGray);

                    SegarkanKartuAyat();
                }
            }
        }
    }
}