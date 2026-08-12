using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using QuranKemenag.Models;
using QuranKemenag.Views;
using System;
namespace QuranKemenag
{
    public sealed partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            NavDashboard.IsChecked = true;
        }

        private void NavDashboard_Checked(object sender, RoutedEventArgs e)
        {
            if (MainContent == null) return;
            BukaDashboard();
        }

        private void NavSurah_Checked(object sender, RoutedEventArgs e)
        {
            if (MainContent == null) return;
            _muatUlangHalamanSaatIni = () => NavSurah_Checked(sender, e);
            HeaderTitle.Text = "Surah";
            MainContent.Content = new SurahListView(this);
        }

        private void NavJuz_Checked(object sender, RoutedEventArgs e)
        {
            if (MainContent == null) return;
            _muatUlangHalamanSaatIni = () => NavJuz_Checked(sender, e);
            HeaderTitle.Text = "Juz";
            MainContent.Content = new JuzListView(this);
        }

        private void NavSettings_Checked(object sender, RoutedEventArgs e)
        {
            if (MainContent == null) return;
            _muatUlangHalamanSaatIni = () => NavSettings_Checked(sender, e);
            HeaderTitle.Text = "Pengaturan Tampilan";
            MainContent.Content = new SettingsView(this);
        }

        private void NavAbout_Checked(object sender, RoutedEventArgs e)
        {
            if (MainContent == null) return;
            _muatUlangHalamanSaatIni = () => NavAbout_Checked(sender, e);
            HeaderTitle.Text = "Tentang Aplikasi";
            MainContent.Content = new AboutView();
        }

        public void BukaDashboard()
        {
            _muatUlangHalamanSaatIni = BukaDashboard;
            MainContent.Content = new DashboardView(this);
            HeaderTitle.Text = "Dashboard";
            if (NavDashboard != null) NavDashboard.IsChecked = true;
        }

        private void UncheckSemuaNavSidebar()
        {
            if (NavDashboard != null) NavDashboard.IsChecked = false;
            if (NavSurah != null) NavSurah.IsChecked = false;
            if (NavJuz != null) NavJuz.IsChecked = false;
            if (NavSettings != null) NavSettings.IsChecked = false;
            if (NavAbout != null) NavAbout.IsChecked = false;
        }

        public void BukaAyatReader(int nomorSurah, int ayatMulai = 1)
        {
            UncheckSemuaNavSidebar();
            MainContent.Content = new AyatReaderView(this, nomorSurah, ayatMulai);
            HeaderTitle.Text = "Baca Ayat";
        }

        public void BukaAyatReaderJuz(Juz juz)
        {
            UncheckSemuaNavSidebar();
            MainContent.Content = new AyatReaderView(this, juz);
            HeaderTitle.Text = "Baca Ayat";
        }

        public void BukaBookmarkList()
        {
            UncheckSemuaNavSidebar();
            MainContent.Content = new BookmarkListView(this);
            HeaderTitle.Text = "Semua Bookmark";
        }

        public void BukaPengaturan()
        {
            if (NavSettings != null) NavSettings.IsChecked = true;
        }

        public void NavSurahDariHalamanLain()
        {
            if (NavSurah != null) NavSurah.IsChecked = true;
            else BukaDashboard();
        }

        public void NavJuzDariHalamanLain()
        {
            if (NavJuz != null) NavJuz.IsChecked = true;
        }

        public void NavDashboardDariHalamanLain()
        {
            BukaDashboard();
        }

        private Action? _muatUlangHalamanSaatIni;

        public void TerapkanTema(ElementTheme tema)
        {
            if (Content is FrameworkElement root)
            {
                root.RequestedTheme = tema;
            }

            // StaticResource cuma di-resolve sekali pas elemen dibuat, jadi halaman yang lagi
            // kebuka (mis. SettingsView sendiri) tidak otomatis ganti warna kecuali di-buat ulang.
            _muatUlangHalamanSaatIni?.Invoke();
        }

        public ElementTheme TemaSaatIni => (Content as FrameworkElement)?.RequestedTheme ?? ElementTheme.Default;

        private bool _sidebarTersembunyi = false;

        private const double LebarSidebarPenuh = 240;
        private const double LebarSidebarMini = 64;

        private void BtnToggleSidebar_Click(object sender, RoutedEventArgs e)
        {
            _sidebarTersembunyi = !_sidebarTersembunyi;
            ColSidebar.Width = new GridLength(_sidebarTersembunyi ? LebarSidebarMini : LebarSidebarPenuh);

            var visibilitasTeks = _sidebarTersembunyi ? Visibility.Collapsed : Visibility.Visible;
            PanelJudulApp.Visibility = visibilitasTeks;
            TxtNavDashboard.Visibility = visibilitasTeks;
            TxtNavSurah.Visibility = visibilitasTeks;
            TxtNavJuz.Visibility = visibilitasTeks;
            TxtNavSettings.Visibility = visibilitasTeks;
            TxtNavAbout.Visibility = visibilitasTeks;
            TxtGroupAlQuran.Visibility = visibilitasTeks;
            TxtGroupPengaturan.Visibility = visibilitasTeks;

            // Header logo: padding kiri dikecilin pas mini, biar logo (40px) muat di kolom 64px
            // tanpa kepotong. Padding normal lagi pas sidebar penuh.
            BorderLogoHeader.Padding = _sidebarTersembunyi ? new Thickness(12, 0, 0, 0) : new Thickness(16, 0, 16, 0);

            // Tooltip nama menu cuma perlu muncul pas mode mini (labelnya kan disembunyikan)
            ToolTipService.SetToolTip(NavDashboard, _sidebarTersembunyi ? "Dashboard" : null);
            ToolTipService.SetToolTip(NavSurah, _sidebarTersembunyi ? "Per Surah" : null);
            ToolTipService.SetToolTip(NavJuz, _sidebarTersembunyi ? "Per Juz" : null);
            ToolTipService.SetToolTip(NavSettings, _sidebarTersembunyi ? "Pengaturan Tampilan" : null);
            ToolTipService.SetToolTip(NavAbout, _sidebarTersembunyi ? "Tentang Aplikasi" : null);

            IkonToggleSidebar.Glyph = _sidebarTersembunyi ? "\uE700" : "\uE76B";
        }
    }
}