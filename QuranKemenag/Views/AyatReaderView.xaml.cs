using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Windows.ApplicationModel;
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage;
using Windows.Storage.Pickers;
using QuranKemenag.Data.Database;
using QuranKemenag.Models;
using QuranKemenag.Services;
using Windows.UI;


namespace QuranKemenag.Views
{
    public sealed partial class AyatReaderView : UserControl
    {
        private readonly MainWindow _mainWindow;
        private int _fontSize = 26;
        private List<Ayat> _ayatSaatIni = new();
        private Surah? _surahSaatIni;
        private List<Surah> _daftarSurah = new();
        private Juz? _juzAktif;
        private List<Juz> _daftarJuz = new();

        // Dipakai lewat x:Bind statis supaya semua kartu di ItemsControl bisa "melihat" status
        // checkbox tampilan tanpa perlu ElementName (yang tidak menembus DataTemplate di WinUI).
        public static bool TampilkanAyatGlobal { get; private set; } = true;
        public static bool TampilkanLatinGlobal { get; private set; } = true;
        public static bool TampilkanTerjemahGlobal { get; private set; } = true;
        public static double UkuranFontGlobal { get; private set; } = 26;
        // Selected font for Arabic text. Static so DataTemplate x:Bind can access it.
        public static FontFamily SelectedFontFamily { get; private set; } = new FontFamily("ms-appx:///Assets/Fonts/LPMQ IsepMisbah.ttf#LPMQ IsepMisbah");

        private bool _fontsLoaded;

        public AyatReaderView(MainWindow mainWindow, int nomorSurahAwal, int ayatMulai = 1)
        {
            InitializeComponent();
            _mainWindow = mainWindow;

            UkuranFontGlobal = _fontSize;

            Loaded += async (s, e) =>
            {
                await LoadFontsFromAssetsAsync();
                await InisialisasiAsync(nomorSurahAwal, ayatMulai);
            };
        }

        /// <summary>Constructor khusus dibuka dari halaman Per Juz — bisa menampilkan ayat lintas-surah.</summary>
        /// <summary>Constructor khusus dibuka dari halaman Per Juz — bisa menampilkan ayat lintas-surah.</summary>
        public AyatReaderView(MainWindow mainWindow, Juz juz)
        {
            InitializeComponent();
            _mainWindow = mainWindow;
            _juzAktif = juz;

            UkuranFontGlobal = _fontSize;

            Loaded += async (s, e) =>
            {
                await LoadFontsFromAssetsAsync();
                _daftarSurah = await QuranDatabase.AmbilDaftarSurahAsync();
                _daftarJuz = await QuranDatabase.AmbilDaftarJuzAsync();

                // Toolbar: sembunyikan kontrol per-surah, tampilkan dropdown per-juz sebagai gantinya.
                CmbSurah.Visibility = Visibility.Collapsed;
                CmbJuz.Visibility = Visibility.Visible;
                CmbAyatDari.Visibility = Visibility.Collapsed;
                TxtSd.Visibility = Visibility.Collapsed;
                CmbAyatSampai.Visibility = Visibility.Collapsed;
                BtnTampilkan.Visibility = Visibility.Collapsed;

                CmbJuz.ItemsSource = _daftarJuz;
                CmbJuz.SelectedValue = juz.Nomor;

                await TampilkanJuzAsync(juz);
            };
        }

        private async void CmbJuz_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (CmbJuz.SelectedValue is int nomorJuz)
            {
                var juzTerpilih = _daftarJuz.FirstOrDefault(j => j.Nomor == nomorJuz);
                if (juzTerpilih != null)
                {
                    _juzAktif = juzTerpilih;
                    await TampilkanJuzAsync(juzTerpilih);
                }
            }
        }
        private async Task TampilkanJuzAsync(Juz juz)
        {

            var hasil = new List<Ayat>();

            for (int nomorSurah = juz.SurahMulai; nomorSurah <= juz.SurahSelesai; nomorSurah++)
            {
                var surahInfo = _daftarSurah.FirstOrDefault(s => s.Nomor == nomorSurah);
                if (surahInfo == null) continue;

                int dari = nomorSurah == juz.SurahMulai ? juz.AyatMulai : 1;
                int sampai = nomorSurah == juz.SurahSelesai ? juz.AyatSelesai : surahInfo.JumlahAyat;

                // Banner pembatas nama surah (ditampilkan untuk setiap surah, termasuk yang pertama,
                // supaya jelas juz ini dimulai dari surah & ayat berapa).
                var pembatas = BuatPembatasSurah(surahInfo, $"{surahInfo.NamaLatin} ({surahInfo.Arti}) • Ayat {dari}-{sampai}");
                hasil.Add(pembatas);


                var semuaAyatSurahIni = await QuranDatabase.AmbilAyatSurahAsync(nomorSurah);
                var terpilih = semuaAyatSurahIni.Where(a => a.NomorAyat >= dari && a.NomorAyat <= sampai).ToList();

                foreach (var ayat in terpilih)
                {
                    ayat.WarnaBookmark = BookmarkService.SudahDisimpan(nomorSurah, ayat.NomorAyat)
                        ? new SolidColorBrush(Color.FromArgb(255, 0x1E, 0x71, 0x45))
                        : new SolidColorBrush(Colors.Transparent);
                }

                hasil.AddRange(terpilih);
            }



            _ayatSaatIni = hasil;
            _surahSaatIni = _daftarSurah.FirstOrDefault(s => s.Nomor == juz.SurahMulai);

            AyatItemsControl.ItemsSource = null;
            AyatItemsControl.ItemsSource = _ayatSaatIni;

            // Mode "Per Halaman" (mushaf) belum mendukung tampilan lintas-surah — dikunci ke Per Ayat.
            bool lintasSurah = juz.SurahMulai != juz.SurahSelesai;
            CmbModeTampilan.SelectedIndex = 0;
            CmbModeTampilan.IsEnabled = !lintasSurah;
        }
        private static string KeAngkaArabTeks(int angka)
        {
            string[] digitArab = { "٠", "١", "٢", "٣", "٤", "٥", "٦", "٧", "٨", "٩" };
            var hasil = new StringBuilder();
            foreach (char c in angka.ToString()) hasil.Append(digitArab[c - '0']);
            return hasil.ToString();
        }
        private static string KeArabMakkiyahMadaniyah(string tempatTurun)
        {
            return tempatTurun.Contains("makk", StringComparison.OrdinalIgnoreCase) ? "مكية" : "مدنية";
        }

        /// <summary>Membuat entri "pembatas" (banner nama surah + Bismillah) untuk 1 surah.</summary>
        private static Ayat BuatPembatasSurah(Surah surah, string labelRentang)
        {
            bool tampilkanBismillah = surah.Nomor != 1 && surah.Nomor != 9;

            return new Ayat
            {
                NomorSurah = surah.Nomor,
                IsPembatasSurah = true,
                NamaSurahUntukPembatas = labelRentang,
                NamaSurahArabUntukPembatas = surah.NamaArab,
                TempatTurunUntukPembatas = KeArabMakkiyahMadaniyah(surah.TempatTurun),
                JumlahAyatArabUntukPembatas = KeAngkaArabTeks(surah.JumlahAyat),
                TampilkanBismillah = tampilkanBismillah
            };
        }
        private async Task InisialisasiAsync(int nomorSurahAwal, int ayatMulai)
        {
            _daftarSurah = await QuranDatabase.AmbilDaftarSurahAsync();
            CmbSurah.ItemsSource = _daftarSurah;
            CmbSurah.SelectedValue = nomorSurahAwal;

            await MuatSurahAsync(nomorSurahAwal, ayatMulai);
        }

        private void CmbJenisFont_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (CmbJenisFont == null) return;

            if (CmbJenisFont.SelectedItem is ComboBoxItem item && item.Tag is string uri)
            {
                try
                {
                    SelectedFontFamily = new FontFamily(uri);
                }
                catch
                {
                    // ignore invalid uri
                }

                SegarkanKartuAyat();
            }
        }

        private async Task LoadFontsFromAssetsAsync()
        {
            try
            {
                if (_fontsLoaded) return;

                var basePath = Windows.ApplicationModel.Package.Current.InstalledLocation.Path;
                var fontsDir = Path.Combine(basePath, "Assets", "Fonts");
                if (!Directory.Exists(fontsDir)) return;

                var filePaths = Directory.GetFiles(fontsDir)
                    .Where(f => f.EndsWith(".ttf", StringComparison.OrdinalIgnoreCase) || f.EndsWith(".otf", StringComparison.OrdinalIgnoreCase))
                    .ToList();

                CmbJenisFont.Items.Clear();

                var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                foreach (var fp in filePaths)
                {
                    var name = Path.GetFileName(fp);
                    var raw = Path.GetFileNameWithoutExtension(name);
                    var displayName = raw.Replace('_', ' ');
                    displayName = Regex.Replace(displayName, "[-_]?Regular$", "", RegexOptions.IgnoreCase).Trim();
                    displayName = Regex.Replace(displayName, "([a-z])([A-Z])", "$1 $2");
                    if (string.IsNullOrWhiteSpace(displayName)) displayName = raw;
                    if (seen.Contains(displayName)) continue;

                    seen.Add(displayName);
                    var uri = $"ms-appx:///Assets/Fonts/{name}#{displayName}";
                    var item = new ComboBoxItem { Content = displayName, Tag = uri };
                    CmbJenisFont.Items.Add(item);
                }

                if (CmbJenisFont.Items.Count > 0)
                {
                    // Cari font LPMQ Isep Misbah untuk dijadikan default; kalau tidak ketemu, pakai yang pertama.
                    int indeksDefault = 0;
                    for (int i = 0; i < CmbJenisFont.Items.Count; i++)
                    {
                        if (CmbJenisFont.Items[i] is ComboBoxItem itemCek &&
                            itemCek.Content is string namaFont &&
                            namaFont.Contains("LPMQ", StringComparison.OrdinalIgnoreCase))
                        {
                            indeksDefault = i;
                            break;
                        }
                    }

                    CmbJenisFont.SelectedIndex = indeksDefault;
                    if (CmbJenisFont.SelectedItem is ComboBoxItem terpilih && terpilih.Tag is string uriTerpilih)
                    {
                        try { SelectedFontFamily = new FontFamily(uriTerpilih); } catch { }
                    }
                }

                _fontsLoaded = true;
            }
            catch
            {
                // ignore
            }
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
                        ? new SolidColorBrush(Color.FromArgb(255, 0x1E, 0x71, 0x45))
                        : new SolidColorBrush(Colors.Transparent);
                }

                // Banner nama surah + Bismillah cuma muncul kalau tampilan dimulai dari ayat 1
                // (bukan pas user pilih rentang custom yang tidak menyertakan ayat pertama).
                if (dari == 1 && _surahSaatIni != null)
                {
                    var pembatas = BuatPembatasSurah(_surahSaatIni, $"{_surahSaatIni.NamaLatin} ({_surahSaatIni.Arti}) • {_surahSaatIni.JumlahAyat} Ayat");
                    _ayatSaatIni.Insert(0, pembatas);
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
            if (_juzAktif != null)
                _mainWindow.NavJuzDariHalamanLain();
            else
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

        private void SalinKePapanKlip(string teksPolos, string rtf)
        {
            var paket = new DataPackage();
            paket.SetText(teksPolos);
            paket.SetRtf(rtf);
            Clipboard.SetContent(paket);
        }

        private void SalinAyat_Click(object sender, RoutedEventArgs e)
        {
            var daftar = _ayatSaatIni.Where(a => !a.IsPembatasSurah).ToList();
            if (daftar.Count == 0) return;

            var teksPolos = new StringBuilder();
            foreach (var a in daftar) teksPolos.AppendLine(a.TeksArab);

            SalinKePapanKlip(teksPolos.ToString(), BangunRtfAyat(daftar, sertakanTerjemah: false));
        }

        private void SalinDenganTerjemah_Click(object sender, RoutedEventArgs e)
        {
            var daftar = _ayatSaatIni.Where(a => !a.IsPembatasSurah).ToList();
            if (daftar.Count == 0) return;

            var teksPolos = new StringBuilder();
            foreach (var a in daftar)
            {
                teksPolos.AppendLine(a.TeksArab);
                teksPolos.AppendLine(a.Terjemah);
                teksPolos.AppendLine();
            }

            SalinKePapanKlip(teksPolos.ToString(), BangunRtfAyat(daftar, sertakanTerjemah: true));
        }

        /// <summary>
        /// Membangun dokumen RTF supaya font LPMQ Isep Misbah (dan arah teks RTL) ikut
        /// terbawa saat ayat di-paste ke Word/aplikasi lain — plain text saja tidak bisa bawa info font.
        /// </summary>
        private string BangunRtfAyat(List<Ayat> daftar, bool sertakanTerjemah)
        {
            var sb = new StringBuilder();
            sb.Append(@"{\rtf1\ansi\ansicpg1252\deff0\deflang1033");
            sb.Append(@"{\fonttbl{\f0 LPMQ Isep Misbah;}{\f1 Calibri;}}");
            sb.Append(@"\viewkind4\uc1");

            int fsArab = _fontSize * 2; // satuan RTF: half-point

            foreach (var ayat in daftar)
            {
                sb.Append(@"\pard\rtlpar\qr\rtlch\f0\fs").Append(fsArab).Append(' ');
                sb.Append(KeRtfUnicode(ayat.TeksArab));
                sb.Append(@"\par");

                if (sertakanTerjemah && !string.IsNullOrWhiteSpace(ayat.Terjemah))
                {
                    sb.Append(@"\pard\ltrpar\ql\ltrch\f1\fs22 ");
                    sb.Append(KeRtfUnicode(ayat.Terjemah));
                    sb.Append(@"\par");
                }

                sb.Append(@"\par");
            }

            sb.Append('}');
            return sb.ToString();
        }

        /// <summary>Escape karakter khusus RTF dan ubah tiap karakter non-ASCII (Arab, dsb) jadi kode \u RTF.</summary>
        private static string KeRtfUnicode(string teks)
        {
            var sb = new StringBuilder();
            foreach (char c in teks)
            {
                if (c == '\\' || c == '{' || c == '}')
                {
                    sb.Append('\\').Append(c);
                }
                else if (c < 128)
                {
                    sb.Append(c);
                }
                else
                {
                    sb.Append(@"\u").Append((int)c).Append('?');
                }
            }
            return sb.ToString();
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

            // Word sedang terbuka -> sisipkan langsung ke posisi kursor di dokumen aktif
            if (WordInteropService.WordSedangTerbuka())
            {
                try
                {
                    WordInteropService.SisipkanKeDokumenAktif(
                        judulSurah: JudulSurahUntukEkspor(),
                        daftarAyat: _ayatSaatIni,
                        tampilkanAyat: ChkAyat.IsChecked == true,
                        tampilkanLatin: ChkLatin.IsChecked == true,
                        tampilkanTerjemah: ChkTerjemah.IsChecked == true,
                        ukuranFontArab: _fontSize
                    );
                }
                catch (Exception ex)
                {
                    await TampilkanDialogAsync("Ekspor Word", $"Gagal menyisipkan ke Word:\n{ex.Message}");
                }
                return;
            }

            // Word tidak terbuka -> proses seperti biasa (buat file .docx baru)
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
            if (sender is Button btn && btn.Tag is Ayat ayat && !ayat.IsPembatasSurah)
            {
                string namaSurah = _daftarSurah.FirstOrDefault(s => s.Nomor == ayat.NomorSurah)?.NamaLatin
                    ?? _surahSaatIni?.NamaLatin ?? "";

                BookmarkService.Toggle(ayat.NomorSurah, ayat.NomorAyat, namaSurah);

                ayat.WarnaBookmark = BookmarkService.SudahDisimpan(ayat.NomorSurah, ayat.NomorAyat)
                    ? new SolidColorBrush(Color.FromArgb(255, 0x1E, 0x71, 0x45))
                    : new SolidColorBrush(Colors.Transparent);

                SegarkanKartuAyat();
            }
        }



        private bool _pengaturanTersembunyi = false;

        private void BtnTogglePengaturan_Click(object sender, RoutedEventArgs e)
        {
            _pengaturanTersembunyi = !_pengaturanTersembunyi;
            ColPengaturan.Width = _pengaturanTersembunyi ? new GridLength(0) : new GridLength(270);
            BtnTogglePengaturan.Visibility = _pengaturanTersembunyi ? Visibility.Visible : Visibility.Collapsed;
        }


    }
}