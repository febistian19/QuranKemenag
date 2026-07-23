using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Media;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using Windows.Storage;

namespace QuranKemenag.Views
{
    public sealed partial class SettingsView : UserControl
    {
        private readonly MainWindow _mainWindow;
        private bool _fontsLoaded;

        public SettingsView(MainWindow mainWindow)
        {
            InitializeComponent();
            _mainWindow = mainWindow;
            SetRadioTemaTanpaTrigger();
            Loaded += async (s, e) => await LoadFontsFromAssetsAsync();
        }

        private void SliderArab_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
        {
            if (PreviewArab == null || TxtUkuranArab == null) return;
            PreviewArab.FontSize = e.NewValue;
            TxtUkuranArab.Text = $"{(int)e.NewValue}px";
        }

        private async Task LoadFontsFromAssetsAsync()
        {
            try
            {
                if (_fontsLoaded) return;
                // Use package installed path and System.IO to enumerate embedded font files
                var basePath = Package.Current.InstalledLocation.Path;
                var fontsDir = Path.Combine(basePath, "Assets", "Fonts");

                if (!Directory.Exists(fontsDir)) return;

                var filePaths = Directory.GetFiles(fontsDir)
                    .Where(f => f.EndsWith(".ttf", StringComparison.OrdinalIgnoreCase) || f.EndsWith(".otf", StringComparison.OrdinalIgnoreCase))
                    .ToList();

                CmbFontMushaf.Items.Clear();

                var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                foreach (var fp in filePaths)
                {
                    var name = Path.GetFileName(fp);
                    var raw = Path.GetFileNameWithoutExtension(name);
                    // Normalize display name: replace underscores, remove common suffixes, split camel case
                    var displayName = raw.Replace('_', ' ');
                    displayName = Regex.Replace(displayName, "[-_]?Regular$", "", RegexOptions.IgnoreCase).Trim();
                    displayName = Regex.Replace(displayName, "([a-z])([A-Z])", "$1 $2");

                    if (string.IsNullOrWhiteSpace(displayName)) displayName = raw;
                    if (seen.Contains(displayName)) continue;

                    seen.Add(displayName);

                    var uri = $"ms-appx:///Assets/Fonts/{name}#{displayName}";
                    var item = new ComboBoxItem { Content = displayName, Tag = uri };
                    CmbFontMushaf.Items.Add(item);
                }

                if (CmbFontMushaf.Items.Count > 0)
                {
                    CmbFontMushaf.SelectedIndex = 0;
                }
                _fontsLoaded = true;
            }
            catch
            {
                // Ignore errors enumerating package files
            }
        }

        private void CmbFontMushaf_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (CmbFontMushaf == null || PreviewArab == null) return;

            if (CmbFontMushaf.SelectedItem is ComboBoxItem item && item.Tag is string uri)
            {
                try
                {
                    PreviewArab.FontFamily = new FontFamily(uri);
                }
                catch
                {
                    // ignore invalid font family
                }
            }
        }

        private void SliderTerjemah_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
        {
            if (PreviewTerjemah == null || TxtUkuranTerjemah == null) return;
            PreviewTerjemah.FontSize = e.NewValue;
            TxtUkuranTerjemah.Text = $"{(int)e.NewValue}px";
        }

        private void RbTema_Checked(object sender, RoutedEventArgs e)
        {
            if (sender is not RadioButton rb) return;

            ElementTheme tema = rb.Name switch
            {
                "RbTerang" => ElementTheme.Light,
                "RbGelap" => ElementTheme.Dark,
                "RbSistem" => ElementTheme.Default,
                _ => ElementTheme.Light
            };

            _mainWindow.TerapkanTema(tema);
        }

        private void SetRadioTemaTanpaTrigger()
        {
            var temaAktif = _mainWindow.TemaSaatIni;

            RbTerang.Checked -= RbTema_Checked;
            RbGelap.Checked -= RbTema_Checked;
            RbSistem.Checked -= RbTema_Checked;

            switch (temaAktif)
            {
                case ElementTheme.Dark:
                    RbGelap.IsChecked = true;
                    break;
                case ElementTheme.Default:
                    RbSistem.IsChecked = true;
                    break;
                default:
                    RbTerang.IsChecked = true;
                    break;
            }

            RbTerang.Checked += RbTema_Checked;
            RbGelap.Checked += RbTema_Checked;
            RbSistem.Checked += RbTema_Checked;
        }
    }
}