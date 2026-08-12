using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Media;

namespace QuranKemenag.Views
{
    public sealed partial class SettingsView : UserControl
    {
        private readonly MainWindow _mainWindow;

        public SettingsView(MainWindow mainWindow)
        {
            InitializeComponent();
            _mainWindow = mainWindow;
            SetRadioTemaTanpaTrigger();
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