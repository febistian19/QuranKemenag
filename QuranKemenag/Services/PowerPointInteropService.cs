using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using QuranKemenag.Models;

namespace QuranKemenag.Services
{
    public static class PowerPointInteropService
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

        private static dynamic? DapatkanPowerPointAktif()
        {
            return GetActiveObjectManual("PowerPoint.Application");
        }

        public static bool PowerPointSedangTerbuka()
        {
            var app = DapatkanPowerPointAktif();
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

        private const int ppLayoutBlank = 12;
        private const int ppAlignLeft = 1;
        private const int ppAlignCenter = 2;
        private const int ppAlignRight = 3;
        private const int msoTextOrientationHorizontal = 1;
        private const int msoTrue = -1;

        private const float LebarSlidePt = 960f;
        private const float MarginPt = 54f;

        public static void SisipkanKeDokumenAktif(
            List<Ayat> daftarAyat,
            bool tampilkanLatin,
            bool tampilkanTerjemah,
            string namaFontArab = "LPMQ Isep Misbah")
        {
            // Slide Bismillah/banner surah dihilangkan total — hanya ayat yang disisipkan.
            var ayatSaja = daftarAyat.Where(a => !a.IsPembatasSurah).ToList();

            dynamic? app = DapatkanPowerPointAktif() ?? throw new InvalidOperationException("Microsoft PowerPoint tidak sedang terbuka atau tidak dapat diakses.");
            dynamic? pres = null;
            dynamic? slides = null;

            try
            {
                pres = app.ActivePresentation;
                if (pres is null)
                    throw new InvalidOperationException("Tidak ada presentasi PowerPoint yang sedang aktif.");

                slides = pres.Slides;
                if (slides is null)
                    throw new InvalidOperationException("Daftar slide PowerPoint tidak dapat diakses.");

                foreach (var ayat in ayatSaja)
                {
                    int indexBaru = slides.Count + 1;
                    dynamic slide = slides.Add(indexBaru, ppLayoutBlank);
                    try
                    {
                        TambahkanSlideAyat(slide, ayat, tampilkanLatin, tampilkanTerjemah, namaFontArab);
                    }
                    finally
                    {
                        LepaskanCom(slide);
                    }
                }

                app.Activate();
            }
            finally
            {
                LepaskanCom(slides);
                LepaskanCom(pres);
                LepaskanCom(app);
            }
        }

        private static void TambahkanSlideAyat(dynamic slide, Ayat ayat, bool tampilkanLatin, bool tampilkanTerjemah, string namaFontArab)
        {
            float lebarKonten = LebarSlidePt - (2 * MarginPt);

            if (ayat.NomorAyat > 0)
            {
                dynamic kotakLabel = slide.Shapes.AddTextbox(
                    msoTextOrientationHorizontal, MarginPt, 24f, lebarKonten, 30f);
                dynamic rangeLabel = kotakLabel.TextFrame.TextRange;
                rangeLabel.Text = $"Ayat {ayat.NomorAyat}";
                rangeLabel.Font.Size = 14;
                rangeLabel.Font.Bold = msoTrue;
                rangeLabel.Font.Color.RGB = 0x8E9B0F;
                rangeLabel.ParagraphFormat.Alignment = ppAlignLeft;
            }

            float posisiY = 70f;

            dynamic kotakArab = slide.Shapes.AddTextbox(
                msoTextOrientationHorizontal, MarginPt, posisiY, lebarKonten, 160f);
            dynamic rangeArab = kotakArab.TextFrame.TextRange;
            rangeArab.Text = ayat.TeksArab ?? "";
            rangeArab.Font.Name = namaFontArab;
            rangeArab.Font.Size = 36;
            rangeArab.Font.Bold = msoTrue;
            rangeArab.Font.Color.RGB = 0x372A1F;
            rangeArab.ParagraphFormat.Alignment = ppAlignRight;
            kotakArab.TextFrame.WordWrap = msoTrue;
            posisiY += 170f;

            if (tampilkanLatin && !string.IsNullOrWhiteSpace(ayat.TeksLatin))
            {
                dynamic kotakLatin = slide.Shapes.AddTextbox(
                    msoTextOrientationHorizontal, MarginPt, posisiY, lebarKonten, 60f);
                dynamic rangeLatin = kotakLatin.TextFrame.TextRange;
                rangeLatin.Text = ayat.TeksLatin;
                rangeLatin.Font.Size = 18;
                rangeLatin.Font.Italic = msoTrue;
                rangeLatin.Font.Color.RGB = 0x27A2C9;
                rangeLatin.ParagraphFormat.Alignment = ppAlignLeft;
                kotakLatin.TextFrame.WordWrap = msoTrue;
                posisiY += 70f;
            }

            if (tampilkanTerjemah && !string.IsNullOrWhiteSpace(ayat.Terjemah))
            {
                dynamic kotakTerjemah = slide.Shapes.AddTextbox(
                    msoTextOrientationHorizontal, MarginPt, posisiY, lebarKonten, 120f);
                dynamic rangeTerjemah = kotakTerjemah.TextFrame.TextRange;
                rangeTerjemah.Text = ayat.Terjemah;
                rangeTerjemah.Font.Size = 18;
                rangeTerjemah.Font.Color.RGB = 0x372A1F;
                rangeTerjemah.ParagraphFormat.Alignment = ppAlignLeft;
                kotakTerjemah.TextFrame.WordWrap = msoTrue;
            }
        }
    }
}