using System.Collections.Generic;
using System.Linq;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using QuranKemenag.Models;

// Alias eksplisit: nama tipe ini sama dengan tipe UI WinUI (Microsoft.UI.Xaml.Documents.*),
// jadi harus ditunjuk persis dari DocumentFormat supaya compiler tidak bingung.
using WordDocument = DocumentFormat.OpenXml.Wordprocessing.Document;
using Paragraph = DocumentFormat.OpenXml.Wordprocessing.Paragraph;
using Run = DocumentFormat.OpenXml.Wordprocessing.Run;
using Bold = DocumentFormat.OpenXml.Wordprocessing.Bold;
using Text = DocumentFormat.OpenXml.Wordprocessing.Text;

namespace QuranKemenag.Services
{
    /// <summary>
    /// Kumpulan fungsi untuk mengekspor ayat Al-Qur'an (Arab, Latin, Terjemah)
    /// ke dalam berkas Microsoft Word (.docx) menggunakan Open XML SDK resmi Microsoft.
    /// Tidak butuh Microsoft Word terpasang di komputer pengguna.
    /// </summary>
    public static class WordExportService
    {
        private const string FontArab = "Traditional Arabic";
        private const string FontLatin = "Segoe UI";
        private const string WarnaHijau = "1E7145";
        private const string WarnaEmas = "C9A227";
        private const string WarnaAbu = "6B7280";

        public static void EksporAyat(
            string filePath,
            string judulSurah,
            IEnumerable<Ayat> daftarAyat,
            bool tampilkanAyat = true,
            bool tampilkanLatin = true,
            bool tampilkanTerjemah = true,
            int ukuranFontArab = 28)
        {
            using var dokumen = WordprocessingDocument.Create(filePath, WordprocessingDocumentType.Document);

            var mainPart = dokumen.AddMainDocumentPart();
            mainPart.Document = new Document();
            var body = new Body();

            // ===== Judul dokumen =====
            body.Append(BuatParagrafJudul(judulSurah));
            body.Append(BuatParagrafKosong());

            // ===== Isi per ayat =====
            foreach (var ayat in daftarAyat)
            {
                if (tampilkanAyat)
                    body.Append(BuatParagrafArab(ayat, ukuranFontArab));

                if (tampilkanLatin)
                    body.Append(BuatParagrafLatin(ayat));

                if (tampilkanTerjemah)
                    body.Append(BuatParagrafTerjemah(ayat));

                if (!string.IsNullOrEmpty(ayat.CatatanKaki))
                    body.Append(BuatParagrafCatatanKaki(ayat.CatatanKaki));

                body.Append(BuatParagrafKosong());
            }

            // ===== Footer sumber =====
            body.Append(BuatParagrafFooter());

            // Ukuran halaman A4 + margin standar
            body.Append(new SectionProperties(
                new PageSize { Width = 11906U, Height = 16838U },
                new PageMargin { Top = 1134, Bottom = 1134, Left = 1134, Right = 1134 }
            ));

            mainPart.Document.Append(body);
            mainPart.Document.Save();
        }

        private static Paragraph BuatParagrafJudul(string judul)
        {
            return new Paragraph(
                new ParagraphProperties(new Justification { Val = JustificationValues.Center }),
                new Run(
                    new RunProperties(
                        new RunFonts { Ascii = FontLatin },
                        new Bold(),
                        new FontSize { Val = "32" },
                        new Color { Val = WarnaHijau }
                    ),
                    new Text(judul)
                )
            );
        }

        private static Paragraph BuatParagrafArab(Ayat ayat, int ukuranFont)
        {
            // ukuranFont dalam "pt" tampilan aplikasi, dikonversi ke half-point (unit Word)
            string szHalfPoint = (ukuranFont * 2).ToString();

            return new Paragraph(
                new ParagraphProperties(
                    new Justification { Val = JustificationValues.Right },
                    new BiDi() // paragraf mode kanan-ke-kiri
                ),
                new Run(
                    new RunProperties(
                        new RunFonts { Ascii = FontArab, ComplexScript = FontArab, HighAnsi = FontArab },
                        new FontSize { Val = szHalfPoint },
                        new FontSizeComplexScript { Val = szHalfPoint },
                        new RightToLeftText()
                    ),
                    new Text($"({ayat.NomorAyat}) {ayat.TeksArab}") { Space = SpaceProcessingModeValues.Preserve }
                )
            );
        }

        private static Paragraph BuatParagrafLatin(Ayat ayat)
        {
            return new Paragraph(
                new ParagraphProperties(new SpacingBetweenLines { Before = "80" }),
                new Run(
                    new RunProperties(
                        new RunFonts { Ascii = FontLatin },
                        new Italic(),
                        new FontSize { Val = "22" },
                        new Color { Val = WarnaEmas }
                    ),
                    new Text(ayat.TeksLatin)
                )
            );
        }

        private static Paragraph BuatParagrafTerjemah(Ayat ayat)
        {
            return new Paragraph(
                new Run(
                    new RunProperties(
                        new RunFonts { Ascii = FontLatin },
                        new FontSize { Val = "22" }
                    ),
                    new Text(ayat.Terjemah)
                )
            );
        }

        private static Paragraph BuatParagrafCatatanKaki(string catatan)
        {
            return new Paragraph(
                new Run(
                    new RunProperties(
                        new RunFonts { Ascii = FontLatin },
                        new Italic(),
                        new FontSize { Val = "18" },
                        new Color { Val = WarnaAbu }
                    ),
                    new Text(catatan)
                )
            );
        }

        private static Paragraph BuatParagrafKosong() => new Paragraph();

        private static Paragraph BuatParagrafFooter()
        {
            return new Paragraph(
                new ParagraphProperties(new Justification { Val = JustificationValues.Center }),
                new Run(
                    new RunProperties(
                        new RunFonts { Ascii = FontLatin },
                        new FontSize { Val = "16" },
                        new Color { Val = WarnaAbu }
                    ),
                    new Text("Dihasilkan oleh Qur'an Kemenag Versi Dekstop")
                )
            );
        }
    }
}