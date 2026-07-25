using System;
using Microsoft.UI.Xaml.Data;

namespace QuranKemenag.Converters
{
    /// <summary>
    /// Mengubah angka biasa (1, 2, 3...) jadi angka Arab (١، ٢، ٣...)
    /// untuk badge nomor ayat/surah/juz.
    /// </summary>
    public class IntKeAngkaArabConverter : IValueConverter
    {
        private static readonly char[] AngkaArab = { '٠', '١', '٢', '٣', '٤', '٥', '٦', '٧', '٨', '٩' };

        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value == null) return "";

            string angkaAsli = value.ToString() ?? "";
            var hasil = new System.Text.StringBuilder();

            foreach (char c in angkaAsli)
            {
                hasil.Append(char.IsDigit(c) ? AngkaArab[c - '0'] : c);
            }

            return hasil.ToString();
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}