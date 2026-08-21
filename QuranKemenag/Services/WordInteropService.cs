using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace QuranKemenag.Services
{
    public static class WordInteropService
    {
        [DllImport("oleaut32.dll", PreserveSig = false)]
        private static extern void GetActiveObject(ref Guid rclsid, IntPtr reserved,
            [MarshalAs(UnmanagedType.IUnknown)] out object ppunk);

        [DllImport("ole32.dll", PreserveSig = false)]
        private static extern void CLSIDFromProgID(
            [MarshalAs(UnmanagedType.LPWStr)] string lpszProgID, out Guid lpclsid);

        private static dynamic? GetActiveObjectManual(string progId)
        {
            try
            {
                CLSIDFromProgID(progId, out Guid clsid);
                GetActiveObject(ref clsid, IntPtr.Zero, out object obj);
                return obj;
            }
            catch (COMException)
            {
                return null;
            }
        }

        private static dynamic? DapatkanWordAktif()
        {
            return GetActiveObjectManual("Word.Application");
        }

        public static bool WordSedangTerbuka()
        {
            var app = DapatkanWordAktif();
            if (app != null)
            {
                LepaskanCom(app);
                return true;
            }
            return false;
        }

        private static void LepaskanCom(object? value)
        {
            if (value is not null && Marshal.IsComObject(value))
                Marshal.ReleaseComObject(value);
        }

        public static void SisipkanKeDokumenAktif(
    string judulSurah,
    List<Models.Ayat> daftarAyat,
    /*bool tampilkanAyat,*/ bool tampilkanLatin, bool tampilkanTerjemah,
    int ukuranFontArab,
    string namaFontArab = "LPMQ Isep Misbah")
        {
            dynamic? app = DapatkanWordAktif() ?? throw new InvalidOperationException("Microsoft Word tidak sedang terbuka atau tidak dapat diakses.");
            dynamic? sel = null;

            try
            {
                sel = app.Selection;
                if (sel is null)
                    throw new InvalidOperationException("Posisi kursor Word tidak dapat diakses.");

                sel.ParagraphFormat.SpaceBefore = 0;
                sel.ParagraphFormat.SpaceAfter = 6;

                // Ukuran font di aplikasi (untuk layar) terlalu besar kalau dipakai apa adanya di Word.
                // Dibatasi maksimal 20pt supaya proporsional dengan lebar halaman dokumen.
                int ukuranArabUntukWord = Math.Min(ukuranFontArab, 20);

                const int wdAlignParagraphLeft = 0;
                const int wdAlignParagraphRight = 2;

                foreach (var ayat in daftarAyat)
                {
                    if (/*tampilkanAyat && */!string.IsNullOrWhiteSpace(ayat.TeksArab))
                    {
                        sel.ParagraphFormat.Alignment = wdAlignParagraphRight;
                        sel.Font.Name = namaFontArab;
                        sel.Font.Size = ukuranArabUntukWord;
                        sel.TypeText($"({ayat.NomorAyat}) {ayat.TeksArab}");
                        sel.TypeParagraph();
                    }

                    if (tampilkanLatin && !string.IsNullOrWhiteSpace(ayat.TeksLatin))
                    {
                        sel.ParagraphFormat.Alignment = wdAlignParagraphLeft;
                        sel.Font.Name = "Calibri";
                        sel.Font.Italic = 1;
                        sel.Font.Size = 11;
                        sel.TypeText(ayat.TeksLatin);
                        sel.TypeParagraph();
                        sel.Font.Italic = 0;
                    }

                    if (tampilkanTerjemah && !string.IsNullOrWhiteSpace(ayat.Terjemah))
                    {
                        sel.Font.Name = "Calibri";
                        sel.Font.Size = 11;
                        sel.TypeText(ayat.Terjemah);
                        sel.TypeParagraph();
                    }
                }

                app.Activate();
            }
            finally
            {
                LepaskanCom(sel);
                LepaskanCom(app);
            }
        }
    }
}