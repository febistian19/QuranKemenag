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
using Microsoft.UI.Xaml.Documents;
using DocumentFormat.OpenXml.Drawing.Diagrams;


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
        public static double TinggiBarisGlobal { get; private set; } = 26 * 1.75;
        // Selected font for Arabic text. Static so DataTemplate x:Bind can access it.
        public static FontFamily SelectedFontFamily { get; private set; } = new FontFamily("ms-appx:///Assets/Fonts/LPMQ IsepMisbah.ttf#LPMQ IsepMisbah");
        public static FlowDirection ArahTeksArabGlobal { get; private set; } = FlowDirection.RightToLeft;
        public static TextAlignment PerataanTeksArabGlobal { get; private set; } = TextAlignment.Justify;
        public static TextWrapping PembungkusanTeksArabGlobal { get; private set; } = TextWrapping.Wrap;
        public static bool TampilanNormalGlobal { get; private set; } = true;
        public static bool TampilanUtsmaniGlobal { get; private set; } = false;

        private bool _fontsLoaded;
        private bool _modeUtsmaniAktif = false;

        // URI font Lpmq_Hafs_01 dipakai sebagai penanda "mode Utsmani" — begitu font ini dipilih,
        // sumber data ayat ikut dialihkan ke msi_simbol.db (bukan cuma ganti tampilan font biasa),
        // karena teksnya pakai encoding PUA yang cuma cocok dengan font ini.
        private const string UriFontUtsmani = "ms-appx:///Assets/Fonts/Lpmq_Hafs_01-Regular.ttf#Lpmq_Hafs_01";

        private string? _teksBismillahStandarCache;
        private string? _teksBismillahUtsmaniCache;

        /// <summary>
        /// Bismillah selalu diambil dari ayat 1 Al-Fatihah, dari sumber data yang sesuai mode font
        /// aktif (quran.db biasa, atau msi_simbol.db untuk Utsmani) — bukan hardcode, supaya kalau
        /// datanya diperbaiki di database, Bismillah ikut ter-update otomatis tanpa ubah kode.
        /// Di-cache per mode supaya tidak query berulang tiap kali banner surah muncul.
        /// </summary>
        private async Task<string> AmbilTeksBismillahAsync()
        {
            if (_modeUtsmaniAktif)
            {
                if (_teksBismillahUtsmaniCache != null) return _teksBismillahUtsmaniCache;

                var ayatFatihah = await QuranDatabase.AmbilAyatSurahUtsmaniAsync(1);
                _teksBismillahUtsmaniCache = ayatFatihah.FirstOrDefault(a => a.NomorAyat == 1)?.TeksArab ?? "";
                return _teksBismillahUtsmaniCache;
            }
            else
            {
                if (_teksBismillahStandarCache != null) return _teksBismillahStandarCache;

                var ayatFatihah = await QuranDatabase.AmbilAyatSurahAsync(1);
                string teksAyat1 = ayatFatihah.FirstOrDefault(a => a.NomorAyat == 1)?.TeksArab ?? "";

                string isi = teksAyat1.Trim('\u202E', '\u202C');

                _teksBismillahStandarCache = "\u202E" + isi + "\u202C";
                return _teksBismillahStandarCache;
            }
        }

        private static readonly Dictionary<string, double> FontSizeMultiplier = new()
            {
                { "Utsmani (Simbol)", 1.0 },
                { "LPMQ Isep Misbah", 0.90 },
                { "KFGQPC Uthmanic Script HAFS", 1.0 }
            };

        // ⬇️ TAMBAHAN BARU
        private void TerapkanUkuranFontGlobal()
        {
            string namaFontAktif = (CmbJenisFont?.SelectedItem as ComboBoxItem)?.Content as string ?? "";
            double multiplier = FontSizeMultiplier.GetValueOrDefault(namaFontAktif, 1.0);
            double ukuranAktual = _fontSize * multiplier;

            UkuranFontGlobal = ukuranAktual;
            TinggiBarisGlobal = ukuranAktual * 1.75;
        }

        public AyatReaderView(MainWindow mainWindow, int nomorSurahAwal, int ayatMulai = 1)
        {
            InitializeComponent();
            _mainWindow = mainWindow;

            UkuranFontGlobal = _fontSize;
            TinggiBarisGlobal = _fontSize * 1.75;

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
                string teksBismillahJuz = await AmbilTeksBismillahAsync();
                var pembatas = BuatPembatasSurah(surahInfo, $"{surahInfo.NamaLatin} ({surahInfo.Arti}) • Ayat {dari}-{sampai}", dari, teksBismillahJuz);
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

        private static string EkstrakNamaFont(FontFamily fontFamily)
        {
            var src = fontFamily.Source;
            int idx = src.IndexOf('#');
            return idx >= 0 ? src.Substring(idx + 1) : src;
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
        private static Ayat BuatPembatasSurah(Surah surah, string labelRentang, int ayatMulaiTampil, string teksBismillah)
        {
            // Bismillah cuma tampil kalau ayat yang ditampilkan benar-benar dimulai dari ayat 1
            // surah tersebut (bukan cuma karena kebetulan surah ini "muncul" di tengah rentang juz),
            // dan dikecualikan untuk Al-Fatihah & At-Taubah.
            bool tampilkanBismillah = surah.Nomor != 1 && surah.Nomor != 9 && ayatMulaiTampil == 1;

            return new Ayat
            {
                NomorSurah = surah.Nomor,
                IsPembatasSurah = true,
                NamaSurahUntukPembatas = labelRentang,
                NamaSurahArabUntukPembatas = surah.NamaArab,
                TempatTurunUntukPembatas = KeArabMakkiyahMadaniyah(surah.TempatTurun),
                JumlahAyatArabUntukPembatas = $"آياتها {KeAngkaArabTeks(surah.JumlahAyat)}",
                TampilkanBismillah = tampilkanBismillah,
                TeksBismillah = teksBismillah
            };
        }
        private async Task InisialisasiAsync(int nomorSurahAwal, int ayatMulai)
        {
            _daftarSurah = await QuranDatabase.AmbilDaftarSurahAsync();
            CmbSurah.ItemsSource = _daftarSurah;
            CmbSurah.SelectedValue = nomorSurahAwal;

            await MuatSurahAsync(nomorSurahAwal, ayatMulai);
        }

        private async void CmbJenisFont_SelectionChanged(object sender, SelectionChangedEventArgs e)
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

                bool modeUtsmaniBaru = uri == UriFontUtsmani;
                TampilanNormalGlobal = !modeUtsmaniBaru;
                TampilanUtsmaniGlobal = modeUtsmaniBaru;

                TerapkanUkuranFontGlobal();   // ⬅️ tambahan baru, sebelum if di bawah

                if (modeUtsmaniBaru != _modeUtsmaniAktif)
                {
                    _modeUtsmaniAktif = modeUtsmaniBaru;
                    await TerapkanModeUtsmaniAsync();
                }
                else
                {
                    SegarkanKartuAyat();
                }
            }
        }

        /// <summary>
        /// Membangun ulang isi RichTextBlock font Utsmani per kartu ayat. TeksArab (properti model)
        /// sudah dalam bentuk "kata-katanya dibalik urutannya" (dipakai untuk mode 1-baris seperti
        /// Salin Ayat & Bismillah yang sudah benar). Untuk RichTextBlock yang bisa wrap banyak baris,
        /// dibutuhkan urutan kata ASLI supaya baris tersusun benar — jadi dibalik lagi di sini
        /// (membalik yang sudah dibalik = balik ke urutan asli), lalu dipasang sebagai Run terpisah.
        /// </summary>
        private void TeksUtsmani_Loaded(object sender, RoutedEventArgs e)
        {
            if (sender is not RichTextBlock rtb || rtb.Tag is not string teksSudahBenar) return;
            if (rtb.Blocks.Count == 0 || rtb.Blocks[0] is not Paragraph paragraf) return;

            paragraf.Inlines.Clear();

            var kata = teksSudahBenar.Split(new[] { '\u00A0', ' ' }, StringSplitOptions.RemoveEmptyEntries);

            for (int i = 0; i < kata.Length; i++)
            {
                paragraf.Inlines.Add(new Run { Text = kata[i] });
                if (i < kata.Length - 1) paragraf.Inlines.Add(new Run { Text = "\u00A0" });
            }
        }

        /// <summary>
        /// Saat mode Utsmani dinyalakan: kunci dropdown surah cuma ke yang datanya tersedia
        /// (SurahTersediaUtsmani), lalu muat ulang ayat dari msi_simbol.db.
        /// Saat dimatikan: balikin dropdown surah ke daftar lengkap 114 surah seperti biasa.
        /// </summary>
        private async Task TerapkanModeUtsmaniAsync()
        {
            List<Surah> daftarTampil;
            int nomorAwal;

            if (_modeUtsmaniAktif)
            {
                daftarTampil = _daftarSurah.Where(s => QuranDatabase.SurahTersediaUtsmani.Contains(s.Nomor)).ToList();
                nomorAwal = (_surahSaatIni != null && QuranDatabase.SurahTersediaUtsmani.Contains(_surahSaatIni.Nomor))
                    ? _surahSaatIni.Nomor
                    : QuranDatabase.SurahTersediaUtsmani[0];
            }
            else
            {
                daftarTampil = _daftarSurah;
                nomorAwal = _surahSaatIni?.Nomor ?? 1;
            }

            CmbSurah.SelectionChanged -= CmbSurah_SelectionChanged;
            CmbSurah.ItemsSource = daftarTampil;
            CmbSurah.SelectedValue = nomorAwal;
            CmbSurah.SelectionChanged += CmbSurah_SelectionChanged;

            await MuatSurahAsync(nomorAwal);
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

                    // Lpmq_Hafs_01 itu font simbol (PUA) khusus buat mode Utsmani, bukan font
                    // "biasa" — namanya ditampilkan beda + Tag-nya dipatok ke UriFontUtsmani
                    // supaya CmbJenisFont_SelectionChanged bisa mendeteksi & switch sumber data.
                    bool iniFontUtsmani = raw.Equals("Lpmq_Hafs_01-Regular", StringComparison.OrdinalIgnoreCase);

                    string displayName;
                    string uri;

                    if (iniFontUtsmani)
                    {
                        displayName = "Utsmani (Simbol)";
                        uri = UriFontUtsmani;
                    }
                    else
                    {
                        displayName = raw.Replace('_', ' ');
                        displayName = Regex.Replace(displayName, "[-_]?Regular$", "", RegexOptions.IgnoreCase).Trim();
                        displayName = Regex.Replace(displayName, "([a-z])([A-Z])", "$1 $2");
                        if (string.IsNullOrWhiteSpace(displayName)) displayName = raw;
                        uri = $"ms-appx:///Assets/Fonts/{name}#{displayName}";
                    }

                    if (seen.Contains(displayName)) continue;

                    seen.Add(displayName);
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

                TerapkanUkuranFontGlobal();   // ⬅️ TAMBAHKAN BARIS INI
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
            var semuaAyatSurah = _modeUtsmaniAktif
                ? await QuranDatabase.AmbilAyatSurahUtsmaniAsync(nomorSurah)
                : await QuranDatabase.AmbilAyatSurahAsync(nomorSurah);

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
                    string teksBismillah = await AmbilTeksBismillahAsync();

                    // untuk menghapus nomer ayat di ayat bismilah (1)

                    // SESUDAH
                    if (_modeUtsmaniAktif)
                    {
                        var potonganBismillah = teksBismillah.Split(new[] { '\u00A0', ' ' }, StringSplitOptions.RemoveEmptyEntries);
                        teksBismillah = potonganBismillah.Length > 1
                            ? string.Join(" ", potonganBismillah.Take(potonganBismillah.Length - 1))
                            : teksBismillah;
                    }

                    // end
                    var pembatas = BuatPembatasSurah(_surahSaatIni, $"{_surahSaatIni.NamaLatin} ({_surahSaatIni.Arti}) • {_surahSaatIni.JumlahAyat} Ayat", 1, teksBismillah);
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
                PanelCentangPerAyat == null || BtnSalinTerjemah == null) return;

            bool mushaf = CmbModeTampilan.SelectedIndex == 1;

            PanelTeksArab.Visibility = mushaf ? Visibility.Collapsed : Visibility.Visible;
            PanelMushaf.Visibility = mushaf ? Visibility.Visible : Visibility.Collapsed;
            PanelCentangPerAyat.Visibility = mushaf ? Visibility.Collapsed : Visibility.Visible;
            //PanelCentangPerHalaman.Visibility = mushaf ? Visibility.Visible : Visibility.Collapsed;
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
            TerapkanUkuranFontGlobal();   // ⬅️ ganti 2 baris lama
            SegarkanKartuAyat();
        }

        private void FontKurang_Click(object sender, RoutedEventArgs e)
        {
            _fontSize = Math.Max(_fontSize - 2, 14);
            TxtUkuranFont.Text = _fontSize.ToString();
            TerapkanUkuranFontGlobal();   // ⬅️ ganti 2 baris lama
            SegarkanKartuAyat();
        }

        private void ChkTampilan_Changed(object sender, RoutedEventArgs e)
        {
            // Guard: dipanggil juga saat InitializeComponent() sebelum semua checkbox dibuat.
            if (/*ChkAyat == null || */ChkLatin == null || ChkTerjemah == null) return;

            //TampilkanAyatGlobal = ChkAyat.IsChecked == true;
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

        private async Task<(int dari, int sampai, bool sertakanTerjemah)?> PilihRentangEksporAsync()
        {
            var daftarAyatValid = _ayatSaatIni.Where(a => !a.IsPembatasSurah).ToList();
            if (daftarAyatValid.Count == 0) return null;

            int minAyat = daftarAyatValid.Min(a => a.NomorAyat);
            int maxAyat = daftarAyatValid.Max(a => a.NomorAyat);

            var cmbDari = new ComboBox
            {
                ItemsSource = Enumerable.Range(minAyat, maxAyat - minAyat + 1).ToList(),
                SelectedValue = minAyat,
                HorizontalAlignment = HorizontalAlignment.Stretch
            };
            var cmbSampai = new ComboBox
            {
                ItemsSource = Enumerable.Range(minAyat, maxAyat - minAyat + 1).ToList(),
                SelectedValue = maxAyat,
                HorizontalAlignment = HorizontalAlignment.Stretch
            };
            var chkTerjemah = new CheckBox
            {
                Content = "Sertakan Terjemah",
                IsChecked = true,
                Margin = new Thickness(0, 8, 0, 0)
            };

            var panel = new StackPanel { Spacing = 10 };
            panel.Children.Add(new TextBlock { Text = "Dari Ayat" });
            panel.Children.Add(cmbDari);
            panel.Children.Add(new TextBlock { Text = "Sampai Ayat", Margin = new Thickness(0, 8, 0, 0) });
            panel.Children.Add(cmbSampai);
            panel.Children.Add(chkTerjemah);

            var dialog = new ContentDialog
            {
                Title = "Pilih Rentang Ayat",
                Content = panel,
                PrimaryButtonText = "Ekspor",
                CloseButtonText = "Batal",
                DefaultButton = ContentDialogButton.Primary,
                XamlRoot = this.XamlRoot
            };

            var hasil = await dialog.ShowAsync();
            if (hasil != ContentDialogResult.Primary) return null;

            if (cmbDari.SelectedValue is int dari && cmbSampai.SelectedValue is int sampai)
            {
                if (dari > sampai) (dari, sampai) = (sampai, dari);
                return (dari, sampai, chkTerjemah.IsChecked == true);
            }

            return null;
        }

        private async void EksporWord_Click(object sender, TappedRoutedEventArgs e)
        {
            if (!await AdaAyatUntukEksporAsync()) return;

            var rentang = await PilihRentangEksporAsync();
            if (rentang == null) return;

            var (dari, sampai, sertakanTerjemah) = rentang.Value;

            //var (dari, sampai) = rentang.Value;
            var ayatUntukEkspor = _ayatSaatIni
                .Where(a => a.IsPembatasSurah || (a.NomorAyat >= dari && a.NomorAyat <= sampai))
                .ToList();

            string? kegagalanInterop = null;

            // Word sedang terbuka -> sisipkan langsung ke posisi kursor di dokumen aktif
            if (WordInteropService.WordSedangTerbuka())
            {
                try
                {
                    WordInteropService.SisipkanKeDokumenAktif(
                        judulSurah: JudulSurahUntukEkspor(),
                        daftarAyat: ayatUntukEkspor,
                        tampilkanLatin: ChkLatin.IsChecked == true,
                        tampilkanTerjemah: sertakanTerjemah,
                        ukuranFontArab: _fontSize,
                        namaFontArab: EkstrakNamaFont(SelectedFontFamily)   
                    );
                    return;
                }
                catch (Exception ex)
                {
                    kegagalanInterop = ex.Message;
                }
            }

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
                    daftarAyat: ayatUntukEkspor,
                    tampilkanLatin: ChkLatin.IsChecked == true,
                    tampilkanTerjemah: sertakanTerjemah,
                    ukuranFontArab: _fontSize,
                    namaFontArab: EkstrakNamaFont(SelectedFontFamily)
                );

                await TawarkanBukaFileAsync(file.Path, "Word");
            }
            catch (Exception ex)
            {
                var detailInterop = string.IsNullOrWhiteSpace(kegagalanInterop)
                    ? string.Empty
                    : $"\n\nPenyisipan langsung ke Word gagal: {kegagalanInterop}";
                await TampilkanDialogAsync("Ekspor Word", $"Gagal mengekspor ke Word:\n{ex.Message}{detailInterop}");
            }
        }

        private async void EksporPpt_Click(object sender, TappedRoutedEventArgs e)
        {
            if (!await AdaAyatUntukEksporAsync()) return;

            var rentang = await PilihRentangEksporAsync();
            if (rentang == null) return;

            var (dari, sampai, sertakanTerjemah) = rentang.Value;
            var ayatUntukEkspor = _ayatSaatIni
                .Where(a => a.IsPembatasSurah || (a.NomorAyat >= dari && a.NomorAyat <= sampai))
                .ToList();

            string? kegagalanInterop = null;

            // PowerPoint sedang terbuka -> sisipkan slide baru ke presentasi aktif
            if (PowerPointInteropService.PowerPointSedangTerbuka())
            {
                try
                {
                    PowerPointInteropService.SisipkanKeDokumenAktif(
                        daftarAyat: ayatUntukEkspor,
                        tampilkanLatin: ChkLatin.IsChecked == true,
                        tampilkanTerjemah: sertakanTerjemah,
                        namaFontArab: EkstrakNamaFont(SelectedFontFamily)
                    );
                    return;
                }
                catch (Exception ex)
                {
                    kegagalanInterop = ex.Message;
                }
            }

            // PowerPoint tidak tersedia atau penyisipan gagal -> buat file .pptx baru.
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
                    daftarAyat: ayatUntukEkspor,
                    tampilkanLatin: ChkLatin.IsChecked == true,
                    tampilkanTerjemah: sertakanTerjemah,
                    namaFontArab: EkstrakNamaFont(SelectedFontFamily)
                );

                await TawarkanBukaFileAsync(file.Path, "PowerPoint");
            }
            catch (Exception ex)
            {
                var detailInterop = string.IsNullOrWhiteSpace(kegagalanInterop)
                    ? string.Empty
                    : $"\n\nPenyisipan langsung ke PowerPoint gagal: {kegagalanInterop}";
                await TampilkanDialogAsync("Ekspor PowerPoint", $"Gagal mengekspor ke PowerPoint:\n{ex.Message}{detailInterop}");
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