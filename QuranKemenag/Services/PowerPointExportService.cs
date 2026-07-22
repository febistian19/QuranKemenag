using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using QuranKemenag.Models;

namespace QuranKemenag.Services
{
    /// <summary>
    /// Mengekspor ayat ke berkas PowerPoint (.pptx) — satu slide judul + satu slide per ayat.
    /// Dibangun langsung sebagai paket OPC (ZIP + XML) mengikuti struktur minimal .pptx yang valid,
    /// tanpa bergantung pada API DocumentFormat.OpenXml.Presentation yang jauh lebih kompleks
    /// dibanding API Wordprocessing. Tidak butuh Microsoft PowerPoint terpasang untuk membuatnya.
    /// </summary>
    public static class PowerPointExportService
    {
        private const int LebarSlide = 12192000;  // EMU, 16:9
        private const int TinggiSlide = 6858000;

        public static void EksporAyat(
            string filePath,
            string judulSurah,
            IEnumerable<Ayat> daftarAyat,
            bool tampilkanLatin = true,
            bool tampilkanTerjemah = true)
        {
            var ayatList = daftarAyat.ToList();

            if (File.Exists(filePath)) File.Delete(filePath);

            using var zip = ZipFile.Open(filePath, ZipArchiveMode.Create);

            TulisEntri(zip, "[Content_Types].xml", BuatContentTypes(ayatList.Count));
            TulisEntri(zip, "_rels/.rels", RelsDasar);
            TulisEntri(zip, "docProps/core.xml", BuatCoreProps(judulSurah));
            TulisEntri(zip, "docProps/app.xml", BuatAppProps(ayatList.Count));

            TulisEntri(zip, "ppt/presentation.xml", BuatPresentation(ayatList.Count));
            TulisEntri(zip, "ppt/_rels/presentation.xml.rels", BuatPresentationRels(ayatList.Count));

            TulisEntri(zip, "ppt/theme/theme1.xml", ThemeXml);
            TulisEntri(zip, "ppt/slideMasters/slideMaster1.xml", SlideMasterXml);
            TulisEntri(zip, "ppt/slideMasters/_rels/slideMaster1.xml.rels", SlideMasterRels);
            TulisEntri(zip, "ppt/slideLayouts/slideLayout1.xml", SlideLayoutXml);
            TulisEntri(zip, "ppt/slideLayouts/_rels/slideLayout1.xml.rels", SlideLayoutRels);

            // Slide 1: judul
            TulisEntri(zip, "ppt/slides/slide1.xml", BuatSlideJudul(judulSurah, ayatList.Count));
            TulisEntri(zip, "ppt/slides/_rels/slide1.xml.rels", SlideRels);

            // Slide per ayat
            for (int i = 0; i < ayatList.Count; i++)
            {
                int nomorSlide = i + 2;
                TulisEntri(zip, $"ppt/slides/slide{nomorSlide}.xml",
                    BuatSlideAyat(ayatList[i], tampilkanLatin, tampilkanTerjemah));
                TulisEntri(zip, $"ppt/slides/_rels/slide{nomorSlide}.xml.rels", SlideRels);
            }
        }

        private static void TulisEntri(ZipArchive zip, string namaEntri, string isiXml)
        {
            var entri = zip.CreateEntry(namaEntri, CompressionLevel.Optimal);
            using var writer = new StreamWriter(entri.Open(), new UTF8Encoding(false));
            writer.Write(isiXml);
        }

        private static string EscapeXml(string teks)
        {
            if (string.IsNullOrEmpty(teks)) return "";
            return teks
                .Replace("&", "&amp;")
                .Replace("<", "&lt;")
                .Replace(">", "&gt;")
                .Replace("\"", "&quot;")
                .Replace("'", "&apos;");
        }

        // ============================ [Content_Types].xml ============================
        private static string BuatContentTypes(int jumlahAyat)
        {
            var sb = new StringBuilder();
            sb.Append("""<?xml version="1.0" encoding="UTF-8" standalone="yes"?><Types xmlns="http://schemas.openxmlformats.org/package/2006/content-types"><Default Extension="rels" ContentType="application/vnd.openxmlformats-package.relationships+xml"/><Default Extension="xml" ContentType="application/xml"/><Override PartName="/ppt/presentation.xml" ContentType="application/vnd.openxmlformats-officedocument.presentationml.presentation.main+xml"/><Override PartName="/ppt/slideMasters/slideMaster1.xml" ContentType="application/vnd.openxmlformats-officedocument.presentationml.slideMaster+xml"/><Override PartName="/ppt/slideLayouts/slideLayout1.xml" ContentType="application/vnd.openxmlformats-officedocument.presentationml.slideLayout+xml"/><Override PartName="/ppt/theme/theme1.xml" ContentType="application/vnd.openxmlformats-officedocument.theme+xml"/><Override PartName="/docProps/core.xml" ContentType="application/vnd.openxmlformats-package.core-properties+xml"/><Override PartName="/docProps/app.xml" ContentType="application/vnd.openxmlformats-officedocument.extended-properties+xml"/>""");

            int totalSlide = jumlahAyat + 1;
            for (int i = 1; i <= totalSlide; i++)
                sb.Append($"""<Override PartName="/ppt/slides/slide{i}.xml" ContentType="application/vnd.openxmlformats-officedocument.presentationml.slide+xml"/>""");

            sb.Append("</Types>");
            return sb.ToString();
        }

        // ============================ _rels/.rels ============================
        private const string RelsDasar = """<?xml version="1.0" encoding="UTF-8" standalone="yes"?><Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships"><Relationship Id="rId1" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument" Target="ppt/presentation.xml"/><Relationship Id="rId2" Type="http://schemas.openxmlformats.org/package/2006/relationships/metadata/core-properties" Target="docProps/core.xml"/><Relationship Id="rId3" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/extended-properties" Target="docProps/app.xml"/></Relationships>""";

        // ============================ docProps ============================
        private static string BuatCoreProps(string judul)
        {
            string tanggal = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ", CultureInfo.InvariantCulture);
            return $"""<?xml version="1.0" encoding="UTF-8" standalone="yes"?><cp:coreProperties xmlns:cp="http://schemas.openxmlformats.org/package/2006/metadata/core-properties" xmlns:dc="http://purl.org/dc/elements/1.1/" xmlns:dcterms="http://purl.org/dc/terms/" xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance"><dc:title>{EscapeXml(judul)}</dc:title><dc:creator>Qur'an Kemenag Versi Dekstop</dc:creator><cp:lastModifiedBy>Qur'an Kemenag Versi Dekstop</cp:lastModifiedBy><dcterms:created xsi:type="dcterms:W3CDTF">{tanggal}</dcterms:created><dcterms:modified xsi:type="dcterms:W3CDTF">{tanggal}</dcterms:modified></cp:coreProperties>""";
        }

        private static string BuatAppProps(int jumlahAyat)
        {
            int totalSlide = jumlahAyat + 1;
            return $"""<?xml version="1.0" encoding="UTF-8" standalone="yes"?><Properties xmlns="http://schemas.openxmlformats.org/officeDocument/2006/extended-properties"><Application>Qur'an Kemenag Versi Dekstop</Application><PresentationFormat>On-screen Show (16:9)</PresentationFormat><Slides>{totalSlide}</Slides></Properties>""";
        }

        // ============================ ppt/presentation.xml ============================
        private static string BuatPresentation(int jumlahAyat)
        {
            var sb = new StringBuilder();
            sb.Append("""<?xml version="1.0" encoding="UTF-8" standalone="yes"?><p:presentation xmlns:a="http://schemas.openxmlformats.org/drawingml/2006/main" xmlns:r="http://schemas.openxmlformats.org/officeDocument/2006/relationships" xmlns:p="http://schemas.openxmlformats.org/presentationml/2006/main"><p:sldMasterIdLst><p:sldMasterId id="2147483648" r:id="rIdMaster1"/></p:sldMasterIdLst><p:sldIdLst>""");

            int totalSlide = jumlahAyat + 1;
            for (int i = 1; i <= totalSlide; i++)
                sb.Append($"""<p:sldId id="{255 + i}" r:id="rIdSlide{i}"/>""");

            sb.Append($"""</p:sldIdLst><p:sldSz cx="{LebarSlide}" cy="{TinggiSlide}" type="screen16x9"/><p:notesSz cx="6858000" cy="9144000"/></p:presentation>""");
            return sb.ToString();
        }

        private static string BuatPresentationRels(int jumlahAyat)
        {
            var sb = new StringBuilder();
            sb.Append("""<?xml version="1.0" encoding="UTF-8" standalone="yes"?><Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships"><Relationship Id="rIdMaster1" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/slideMaster" Target="slideMasters/slideMaster1.xml"/>""");

            int totalSlide = jumlahAyat + 1;
            for (int i = 1; i <= totalSlide; i++)
                sb.Append($"""<Relationship Id="rIdSlide{i}" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/slide" Target="slides/slide{i}.xml"/>""");

            sb.Append("</Relationships>");
            return sb.ToString();
        }

        // ============================ Theme, master, layout (statis) ============================
        private const string ThemeXml = """<?xml version="1.0" encoding="UTF-8" standalone="yes"?><a:theme xmlns:a="http://schemas.openxmlformats.org/drawingml/2006/main" name="Qur'an Kemenag"><a:themeElements><a:clrScheme name="QuranKemenag"><a:dk1><a:sysClr val="windowText" lastClr="000000"/></a:dk1><a:lt1><a:sysClr val="window" lastClr="FFFFFF"/></a:lt1><a:dk2><a:srgbClr val="14532D"/></a:dk2><a:lt2><a:srgbClr val="E7F4EF"/></a:lt2><a:accent1><a:srgbClr val="1E7145"/></a:accent1><a:accent2><a:srgbClr val="0F9B8E"/></a:accent2><a:accent3><a:srgbClr val="C9A227"/></a:accent3><a:accent4><a:srgbClr val="14532D"/></a:accent4><a:accent5><a:srgbClr val="6B7280"/></a:accent5><a:accent6><a:srgbClr val="9CA3AF"/></a:accent6><a:hlink><a:srgbClr val="0563C1"/></a:hlink><a:folHlink><a:srgbClr val="954F72"/></a:folHlink></a:clrScheme><a:fontScheme name="QuranKemenag"><a:majorFont><a:latin typeface="Calibri Light"/><a:ea typeface=""/><a:cs typeface=""/></a:majorFont><a:minorFont><a:latin typeface="Calibri"/><a:ea typeface=""/><a:cs typeface=""/></a:minorFont></a:fontScheme><a:fmtScheme name="QuranKemenag"><a:fillStyleLst><a:solidFill><a:schemeClr val="phClr"/></a:solidFill><a:solidFill><a:schemeClr val="phClr"/></a:solidFill><a:solidFill><a:schemeClr val="phClr"/></a:solidFill></a:fillStyleLst><a:lnStyleLst><a:ln w="6350"><a:solidFill><a:schemeClr val="phClr"/></a:solidFill></a:ln><a:ln w="12700"><a:solidFill><a:schemeClr val="phClr"/></a:solidFill></a:ln><a:ln w="19050"><a:solidFill><a:schemeClr val="phClr"/></a:solidFill></a:ln></a:lnStyleLst><a:effectStyleLst><a:effectStyle><a:effectLst/></a:effectStyle><a:effectStyle><a:effectLst/></a:effectStyle><a:effectStyle><a:effectLst/></a:effectStyle></a:effectStyleLst><a:bgFillStyleLst><a:solidFill><a:schemeClr val="phClr"/></a:solidFill><a:solidFill><a:schemeClr val="phClr"/></a:solidFill><a:solidFill><a:schemeClr val="phClr"/></a:solidFill></a:bgFillStyleLst></a:fmtScheme></a:themeElements></a:theme>""";

        private const string SlideMasterXml = """<?xml version="1.0" encoding="UTF-8" standalone="yes"?><p:sldMaster xmlns:a="http://schemas.openxmlformats.org/drawingml/2006/main" xmlns:r="http://schemas.openxmlformats.org/officeDocument/2006/relationships" xmlns:p="http://schemas.openxmlformats.org/presentationml/2006/main"><p:cSld><p:spTree><p:nvGrpSpPr><p:cNvPr id="1" name=""/><p:cNvGrpSpPr/><p:nvPr/></p:nvGrpSpPr><p:grpSpPr><a:xfrm><a:off x="0" y="0"/><a:ext cx="0" cy="0"/><a:chOff x="0" y="0"/><a:chExt cx="0" cy="0"/></a:xfrm></p:grpSpPr></p:spTree></p:cSld><p:clrMap bg1="lt1" tx1="dk1" bg2="lt2" tx2="dk2" accent1="accent1" accent2="accent2" accent3="accent3" accent4="accent4" accent5="accent5" accent6="accent6" hlink="hlink" folHlink="folHlink"/><p:sldLayoutIdLst><p:sldLayoutId id="2147483649" r:id="rIdLayout1"/></p:sldLayoutIdLst></p:sldMaster>""";

        private const string SlideMasterRels = """<?xml version="1.0" encoding="UTF-8" standalone="yes"?><Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships"><Relationship Id="rIdLayout1" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/slideLayout" Target="../slideLayouts/slideLayout1.xml"/><Relationship Id="rIdTheme1" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/theme" Target="../theme/theme1.xml"/></Relationships>""";

        private const string SlideLayoutXml = """<?xml version="1.0" encoding="UTF-8" standalone="yes"?><p:sldLayout xmlns:a="http://schemas.openxmlformats.org/drawingml/2006/main" xmlns:r="http://schemas.openxmlformats.org/officeDocument/2006/relationships" xmlns:p="http://schemas.openxmlformats.org/presentationml/2006/main" type="blank" preserve="1"><p:cSld name="Kosong"><p:spTree><p:nvGrpSpPr><p:cNvPr id="1" name=""/><p:cNvGrpSpPr/><p:nvPr/></p:nvGrpSpPr><p:grpSpPr><a:xfrm><a:off x="0" y="0"/><a:ext cx="0" cy="0"/><a:chOff x="0" y="0"/><a:chExt cx="0" cy="0"/></a:xfrm></p:grpSpPr></p:spTree></p:cSld><p:clrMapOvr><a:overrideClrMapping bg1="lt1" tx1="dk1" bg2="lt2" tx2="dk2" accent1="accent1" accent2="accent2" accent3="accent3" accent4="accent4" accent5="accent5" accent6="accent6" hlink="hlink" folHlink="folHlink"/></p:clrMapOvr></p:sldLayout>""";

        private const string SlideLayoutRels = """<?xml version="1.0" encoding="UTF-8" standalone="yes"?><Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships"><Relationship Id="rIdMaster" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/slideMaster" Target="../slideMasters/slideMaster1.xml"/></Relationships>""";

        private const string SlideRels = """<?xml version="1.0" encoding="UTF-8" standalone="yes"?><Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships"><Relationship Id="rId1" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/slideLayout" Target="../slideLayouts/slideLayout1.xml"/></Relationships>""";

        private const string ClrMapOvr = """<p:clrMapOvr><a:overrideClrMapping bg1="lt1" tx1="dk1" bg2="lt2" tx2="dk2" accent1="accent1" accent2="accent2" accent3="accent3" accent4="accent4" accent5="accent5" accent6="accent6" hlink="hlink" folHlink="folHlink"/></p:clrMapOvr>""";

        // ============================ Slide judul ============================
        private static string BuatSlideJudul(string judulSurah, int jumlahAyat)
        {
            string sub = $"{jumlahAyat} ayat  •  Dihasilkan oleh Qur'an Kemenag Versi Dekstop";
            return $"""<?xml version="1.0" encoding="UTF-8" standalone="yes"?><p:sld xmlns:a="http://schemas.openxmlformats.org/drawingml/2006/main" xmlns:r="http://schemas.openxmlformats.org/officeDocument/2006/relationships" xmlns:p="http://schemas.openxmlformats.org/presentationml/2006/main"><p:cSld><p:spTree><p:nvGrpSpPr><p:cNvPr id="1" name=""/><p:cNvGrpSpPr/><p:nvPr/></p:nvGrpSpPr><p:grpSpPr><a:xfrm><a:off x="0" y="0"/><a:ext cx="0" cy="0"/><a:chOff x="0" y="0"/><a:chExt cx="0" cy="0"/></a:xfrm></p:grpSpPr><p:sp><p:nvSpPr><p:cNvPr id="2" name="Judul"/><p:cNvSpPr><a:spLocks noGrp="1"/></p:cNvSpPr><p:nvPr/></p:nvSpPr><p:spPr><a:xfrm><a:off x="685800" y="2514600"/><a:ext cx="10820400" cy="1200150"/></a:xfrm><a:prstGeom prst="rect"><a:avLst/></a:prstGeom></p:spPr><p:txBody><a:bodyPr anchor="ctr"/><a:lstStyle/><a:p><a:pPr algn="ctr"/><a:r><a:rPr lang="id-ID" sz="4000" b="1"><a:solidFill><a:srgbClr val="1E7145"/></a:solidFill></a:rPr><a:t>{EscapeXml(judulSurah)}</a:t></a:r></a:p></p:txBody></p:sp><p:sp><p:nvSpPr><p:cNvPr id="3" name="Sub"/><p:cNvSpPr><a:spLocks noGrp="1"/></p:cNvSpPr><p:nvPr/></p:nvSpPr><p:spPr><a:xfrm><a:off x="685800" y="3714750"/><a:ext cx="10820400" cy="600075"/></a:xfrm><a:prstGeom prst="rect"><a:avLst/></a:prstGeom></p:spPr><p:txBody><a:bodyPr anchor="ctr"/><a:lstStyle/><a:p><a:pPr algn="ctr"/><a:r><a:rPr lang="id-ID" sz="1600"><a:solidFill><a:srgbClr val="6B7280"/></a:solidFill></a:rPr><a:t>{EscapeXml(sub)}</a:t></a:r></a:p></p:txBody></p:sp></p:spTree></p:cSld>{ClrMapOvr}</p:sld>""";
        }

        // ============================ Slide per ayat ============================
        private static string BuatSlideAyat(Ayat ayat, bool tampilkanLatin, bool tampilkanTerjemah)
        {
            var paragraf = new StringBuilder();

            paragraf.Append($"""<a:p><a:pPr algn="r" rtl="1"/><a:r><a:rPr lang="ar-SA" sz="3600" b="1"><a:latin typeface="Traditional Arabic"/><a:cs typeface="Traditional Arabic"/><a:solidFill><a:srgbClr val="1F2937"/></a:solidFill></a:rPr><a:t>{EscapeXml(ayat.TeksArab)}</a:t></a:r></a:p>""");

            if (tampilkanLatin && !string.IsNullOrEmpty(ayat.TeksLatin))
            {
                paragraf.Append($"""<a:p><a:pPr algn="l"><a:spcBef><a:spcPts val="1800"/></a:spcBef></a:pPr><a:r><a:rPr lang="id-ID" sz="1800" i="1"><a:solidFill><a:srgbClr val="C9A227"/></a:solidFill></a:rPr><a:t>{EscapeXml(ayat.TeksLatin)}</a:t></a:r></a:p>""");
            }

            if (tampilkanTerjemah && !string.IsNullOrEmpty(ayat.Terjemah))
            {
                paragraf.Append($"""<a:p><a:pPr algn="l"><a:spcBef><a:spcPts val="1200"/></a:spcBef></a:pPr><a:r><a:rPr lang="id-ID" sz="1800"><a:solidFill><a:srgbClr val="1F2937"/></a:solidFill></a:rPr><a:t>{EscapeXml(ayat.Terjemah)}</a:t></a:r></a:p>""");
            }

            string label = ayat.NomorAyat > 0 ? $"Ayat {ayat.NomorAyat}" : "";

            return $"""<?xml version="1.0" encoding="UTF-8" standalone="yes"?><p:sld xmlns:a="http://schemas.openxmlformats.org/drawingml/2006/main" xmlns:r="http://schemas.openxmlformats.org/officeDocument/2006/relationships" xmlns:p="http://schemas.openxmlformats.org/presentationml/2006/main"><p:cSld><p:spTree><p:nvGrpSpPr><p:cNvPr id="1" name=""/><p:cNvGrpSpPr/><p:nvPr/></p:nvGrpSpPr><p:grpSpPr><a:xfrm><a:off x="0" y="0"/><a:ext cx="0" cy="0"/><a:chOff x="0" y="0"/><a:chExt cx="0" cy="0"/></a:xfrm></p:grpSpPr><p:sp><p:nvSpPr><p:cNvPr id="2" name="Label"/><p:cNvSpPr><a:spLocks noGrp="1"/></p:cNvSpPr><p:nvPr/></p:nvSpPr><p:spPr><a:xfrm><a:off x="685800" y="274638"/><a:ext cx="10820400" cy="457200"/></a:xfrm><a:prstGeom prst="rect"><a:avLst/></a:prstGeom></p:spPr><p:txBody><a:bodyPr/><a:lstStyle/><a:p><a:pPr algn="l"/><a:r><a:rPr lang="id-ID" sz="1400" b="1"><a:solidFill><a:srgbClr val="0F9B8E"/></a:solidFill></a:rPr><a:t>{EscapeXml(label)}</a:t></a:r></a:p></p:txBody></p:sp><p:sp><p:nvSpPr><p:cNvPr id="3" name="Isi"/><p:cNvSpPr><a:spLocks noGrp="1"/></p:cNvSpPr><p:nvPr/></p:nvSpPr><p:spPr><a:xfrm><a:off x="685800" y="914400"/><a:ext cx="10820400" cy="5486400"/></a:xfrm><a:prstGeom prst="rect"><a:avLst/></a:prstGeom></p:spPr><p:txBody><a:bodyPr wrap="square" anchor="t"><a:normAutofit/></a:bodyPr><a:lstStyle/>{paragraf}</p:txBody></p:sp></p:spTree></p:cSld>{ClrMapOvr}</p:sld>""";
        }
    }
}