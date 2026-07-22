using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using QuranKemenag.Models;
using QuranKemenag.Services;

namespace QuranKemenag.Views
{
    public sealed partial class BookmarkListView : UserControl
    {
        private readonly MainWindow _mainWindow;

        public BookmarkListView(MainWindow mainWindow)
        {
            InitializeComponent();
            _mainWindow = mainWindow;
            MuatDaftarBookmark();
        }

        private void MuatDaftarBookmark()
        {
            var semuaBookmark = BookmarkService.AmbilSemua();
            DaftarBookmark.ItemsSource = semuaBookmark;
            TxtKosong.Visibility = semuaBookmark.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
        }

        private void BukaAyat_Tapped(object sender, TappedRoutedEventArgs e)
        {
            if (sender is FrameworkElement fe && fe.Tag is Bookmark bm)
            {
                _mainWindow.BukaAyatReader(bm.NomorSurah, bm.NomorAyat);
            }
        }

        private void HapusBookmark_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is Bookmark bm)
            {
                BookmarkService.Hapus(bm.NomorSurah, bm.NomorAyat);
                MuatDaftarBookmark();
            }
        }

        private void Kembali_Click(object sender, RoutedEventArgs e)
        {
            _mainWindow.BukaDashboard();
        }
    }
}