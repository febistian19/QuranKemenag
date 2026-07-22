using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using QuranKemenag.Views;
using System;
using System.Threading.Tasks;

namespace QuranKemenag
{
    public sealed partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            NavDashboard.IsChecked = true; // set setelah semua elemen sidebar siap
        }

        public void BukaDashboard()
        {
            MainContent.Content = new DashboardView(this);
            HeaderTitle.Text = "Dashboard";
            if (NavDashboard != null) NavDashboard.IsChecked = true;
        }

        public void NavSurahDariHalamanLain()
        {
            // Dipanggil dari tombol "Kembali" di AyatReaderView
            BukaDashboard();
        }

        public void BukaAyatReader(int nomorSurah, int ayatMulai = 1)
        {
            MainContent.Content = new AyatReaderView(this, nomorSurah, ayatMulai);
            HeaderTitle.Text = "Baca Ayat";
            if (NavSurah != null) NavSurah.IsChecked = false; // supaya highlight sidebar tidak "nyangkut" di Per Surah
        }

        public void BukaBookmarkList()
        {
            _ = TampilkanBelumTersediaAsync("Halaman Semua Bookmark belum dimigrasikan ke WinUI.");
        }

        private void NavDashboard_Checked(object sender, RoutedEventArgs e)
        {
            if (MainContent == null) return;
            BukaDashboard();
        }

        private void NavSurah_Checked(object sender, RoutedEventArgs e)
        {
            if (MainContent == null) return;
            _ = TampilkanBelumTersediaAsync("Halaman daftar Per Surah belum dimigrasikan ke WinUI.");
        }

        private void NavJuz_Checked(object sender, RoutedEventArgs e)
        {
            if (MainContent == null) return;
            _ = TampilkanBelumTersediaAsync("Halaman Per Juz belum dimigrasikan ke WinUI.");
        }

        private void NavSettings_Checked(object sender, RoutedEventArgs e)
        {
            if (MainContent == null) return;
            _ = TampilkanBelumTersediaAsync("Halaman Pengaturan Tampilan belum dimigrasikan ke WinUI.");
        }

        private void NavAbout_Checked(object sender, RoutedEventArgs e)
        {
            if (MainContent == null) return;
            _ = TampilkanBelumTersediaAsync("Halaman Tentang Aplikasi belum dimigrasikan ke WinUI.");
        }

        private async Task TampilkanBelumTersediaAsync(string pesan)
        {
            var dialog = new ContentDialog
            {
                Title = "Belum tersedia",
                Content = pesan,
                CloseButtonText = "OK",
                XamlRoot = this.Content.XamlRoot
            };
            await dialog.ShowAsync();

            BukaDashboard();
        }
    }
}