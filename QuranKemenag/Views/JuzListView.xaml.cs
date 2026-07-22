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
    public sealed partial class JuzListView : UserControl
    {
        private readonly MainWindow _mainWindow;
        private List<Juz> _daftarJuz = new();

        public JuzListView(MainWindow mainWindow)
        {
            InitializeComponent();
            _mainWindow = mainWindow;

            Loaded += async (s, e) => await MuatDaftarJuzAsync();
        }

        private async Task MuatDaftarJuzAsync()
        {
            _daftarJuz = await QuranDatabase.AmbilDaftarJuzAsync();
            JuzItemsControl.ItemsSource = _daftarJuz;
        }

        private void TxtCari_TextChanged(object sender, TextChangedEventArgs e)
        {
            var kata = TxtCari.Text.Trim().ToLower();
            JuzItemsControl.ItemsSource = string.IsNullOrEmpty(kata)
                ? _daftarJuz
                : _daftarJuz.Where(j =>
                    j.Rentang.ToLower().Contains(kata) || j.Nomor.ToString().Contains(kata)).ToList();
        }

        private void KartuJuz_Tapped(object sender, TappedRoutedEventArgs e)
        {
            if (sender is FrameworkElement fe && fe.Tag is Juz juz)
            {
                _mainWindow.BukaAyatReader(juz.SurahMulai, juz.AyatMulai);
            }
        }

        private void ToggleSurah_Checked(object sender, RoutedEventArgs e)
        {
            _mainWindow.NavSurahDariHalamanLain();
        }

        private void Kembali_Click(object sender, RoutedEventArgs e)
        {
            _mainWindow.NavDashboardDariHalamanLain();
        }
    }
}