using Microsoft.UI.Xaml;
using QuranKemenag.Views;

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
            HeaderTitle.Text = "Surah";
            MainContent.Content = new SurahListView(this);
        }

        private void NavJuz_Checked(object sender, RoutedEventArgs e)
        {
            if (MainContent == null) return;
            HeaderTitle.Text = "Juz";
            MainContent.Content = new JuzListView(this);
        }

        private void NavSettings_Checked(object sender, RoutedEventArgs e)
        {
            if (MainContent == null) return;
            HeaderTitle.Text = "Pengaturan Tampilan";
            MainContent.Content = new SettingsView(this);
        }

        private void NavAbout_Checked(object sender, RoutedEventArgs e)
        {
            if (MainContent == null) return;
            HeaderTitle.Text = "Tentang Aplikasi";
            MainContent.Content = new AboutView();
        }

        public void BukaDashboard()
        {
            MainContent.Content = new DashboardView(this);
            HeaderTitle.Text = "Dashboard";
            if (NavDashboard != null) NavDashboard.IsChecked = true;
        }

        public void BukaAyatReader(int nomorSurah, int ayatMulai = 1)
        {
            MainContent.Content = new AyatReaderView(this, nomorSurah, ayatMulai);
            HeaderTitle.Text = "Baca Ayat";
            if (NavSurah != null) NavSurah.IsChecked = false;
        }

        public void BukaBookmarkList()
        {
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

        public void TerapkanTema(ElementTheme tema)
        {
            if (Content is FrameworkElement root)
            {
                root.RequestedTheme = tema;
            }
        }

        public ElementTheme TemaSaatIni => (Content as FrameworkElement)?.RequestedTheme ?? ElementTheme.Default;

        private bool _sidebarTersembunyi = false;

        private void BtnToggleSidebar_Click(object sender, RoutedEventArgs e)
        {
            _sidebarTersembunyi = !_sidebarTersembunyi;
            ColSidebar.Width = _sidebarTersembunyi ? new GridLength(0) : new GridLength(240);

            // Icon berubah sesuai status: panah kiri saat terbuka, hamburger saat tertutup
            IkonToggleSidebar.Glyph = _sidebarTersembunyi ? "\uE700" : "\uE76B";
        }
    }
}