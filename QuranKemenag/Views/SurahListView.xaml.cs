using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using QuranKemenag.Data.Database;
using QuranKemenag.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace QuranKemenag.Views
{
    public sealed partial class SurahListView : UserControl
    {
        private readonly MainWindow _mainWindow;
        private List<Surah> _daftarSurah = new();

        public SurahListView(MainWindow mainWindow)
        {
            InitializeComponent();
            _mainWindow = mainWindow;

            Loaded += async (s, e) => await MuatDaftarSurahAsync();
        }

        private async Task MuatDaftarSurahAsync()
        {
            _daftarSurah = await QuranDatabase.AmbilDaftarSurahAsync();
            SurahItemsControl.ItemsSource = _daftarSurah;
        }

        private void TxtCari_TextChanged(object sender, TextChangedEventArgs e)
        {
            var kata = TxtCari.Text.Trim().ToLower();
            SurahItemsControl.ItemsSource = string.IsNullOrEmpty(kata)
                ? _daftarSurah
                : _daftarSurah.Where(s =>
                    s.NamaLatin.ToLower().Contains(kata) || s.Arti.ToLower().Contains(kata)).ToList();
        }

        private void KartuSurah_Tapped(object sender, TappedRoutedEventArgs e)
        {
            if (sender is FrameworkElement fe && fe.Tag is int nomorSurah)
            {
                _mainWindow.BukaAyatReader(nomorSurah);
            }
        }

        private void ToggleJuz_Checked(object sender, RoutedEventArgs e)
        {
            _mainWindow.NavJuzDariHalamanLain();
        }

        private void Kembali_Click(object sender, RoutedEventArgs e)
        {
            _mainWindow.NavDashboardDariHalamanLain();
        }
    }
}