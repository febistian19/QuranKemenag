using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using QuranKemenag.Models;
using QuranKemenag.Services;
using System.Collections.Generic;
using System.Linq;

namespace QuranKemenag.Views
{
    // Kelas pembantu tampilan, supaya x:Bind bisa langsung ambil teks "Ayat X" tanpa multiple Run
    public class BookmarkTampil
    {
        public int NomorSurah { get; set; }
        public int NomorAyat { get; set; }
        public string NamaSurah { get; set; } = string.Empty;
        public string TeksAyat => $"Ayat {NomorAyat}";
    }

    public sealed partial class DashboardView : UserControl
    {
        private readonly MainWindow _mainWindow;

        public DashboardView(MainWindow mainWindow)
        {
            this.InitializeComponent();
            _mainWindow = mainWindow;

            var semuaBookmark = BookmarkService.AmbilSemua();
            BookmarkList.ItemsSource = semuaBookmark.Select(b => new BookmarkTampil
            {
                NomorSurah = b.NomorSurah,
                NomorAyat = b.NomorAyat,
                NamaSurah = b.NamaSurah
            }).ToList();
        }

        private void MulaiEkspor_Click(object sender, RoutedEventArgs e)
        {
            _mainWindow.BukaAyatReader(1);
        }

        private void LihatSemuaBookmark_Click(object sender, RoutedEventArgs e)
        {
            _mainWindow.BukaBookmarkList();
        }

        private void BookmarkItem_Tapped(object sender, TappedRoutedEventArgs e)
        {
            if (sender is FrameworkElement fe && fe.Tag is BookmarkTampil bt)
            {
                _mainWindow.BukaAyatReader(bt.NomorSurah, bt.NomorAyat);
            }
        }
    }
}