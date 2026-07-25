using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using QuranKemenag.Models;
using Windows.Storage;
using System.Linq;
using System.Text.RegularExpressions;

namespace QuranKemenag.Data.Database
{
    /// <summary>
    /// Lapisan akses data SQLite. Database dibundel sebagai Asset bawaan aplikasi
    /// (tidak perlu sinkronisasi online), lalu di-copy ke folder lokal aplikasi
    /// (ApplicationData) supaya bisa dibaca-tulis dengan bebas.
    /// </summary>
    public static class QuranDatabase
    {
        private static string DbPath => Path.Combine(ApplicationData.Current.LocalFolder.Path, "quran.db");

        private static string ConnectionString => $"Data Source={DbPath}";

        /// <summary>
        /// Dipanggil sekali saat aplikasi start. Menyalin database bawaan (Asset)
        /// ke folder lokal aplikasi, hanya jika belum pernah disalin sebelumnya.
        /// </summary>
        public static async Task InisialisasiAsync()
        {
            var assetFile = await StorageFile.GetFileFromApplicationUriAsync(
                new Uri("ms-appx:///Assets/quran.db"));

            var targetFolder = ApplicationData.Current.LocalFolder;
            await assetFile.CopyAsync(targetFolder, "quran.db", NameCollisionOption.ReplaceExisting);
        }

        public static async Task<List<Surah>> AmbilDaftarSurahAsync()
        {
            var daftar = new List<Surah>();

            using var koneksi = new SqliteConnection(ConnectionString);
            await koneksi.OpenAsync();

            var cmd = koneksi.CreateCommand();
            cmd.CommandText = "SELECT id, namalatin, namaarab, terjemah, jumlahayat, kategory FROM namasurah ORDER BY posisi";

            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                daftar.Add(new Surah
                {
                    Nomor = reader.GetInt32(0),
                    NamaLatin = reader.GetString(1),
                    NamaArab = reader.GetString(2).Trim(),
                    Arti = reader.GetString(3),
                    JumlahAyat = reader.GetInt32(4),
                    TempatTurun = reader.GetString(5),
                });
            }

            return daftar;
        }

        public static async Task<List<Ayat>> AmbilAyatSurahAsync(int nomorSurah)
        {
            var daftar = new List<Ayat>();

            using var koneksi = new SqliteConnection(ConnectionString);
            await koneksi.OpenAsync();

            var cmd = koneksi.CreateCommand();
            cmd.CommandText = """
                SELECT q.ayat, q.teks, t.teks AS terjemah, t.footnote
                FROM quran q
                LEFT JOIN terjemah_id t ON t.surah = q.surah AND t.ayat = q.ayat
                WHERE q.surah = $nomor
                ORDER BY q.ayat
                """;
            cmd.Parameters.AddWithValue("$nomor", nomorSurah);

            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                string? footnote = reader.IsDBNull(3) ? null : reader.GetString(3);

                daftar.Add(new Ayat
                {
                    NomorSurah = nomorSurah,
                    NomorAyat = reader.GetInt32(0),
                    TeksArab = reader.GetString(1),
                    TeksLatin = "",       // masih belum tersedia di database ini
                    Terjemah = BersihkanPenandaCatatanKaki(reader.IsDBNull(2) ? "" : reader.GetString(2)),
                    CatatanKaki = string.IsNullOrWhiteSpace(footnote) ? null : footnote,
                });
            }

            return daftar;
        }

        /// <summary>
        /// Data terjemahan sumber kadang menempelkan nomor catatan kaki langsung ke kata
        /// sebelumnya tanpa spasi (mis. "tempatnya206)"). Method ini menyisipkan spasi
        /// otomatis dan mengubah angkanya jadi superscript, supaya kelihatan seperti
        /// nomor catatan kaki pada umumnya, bukan angka biasa yang menempel di kalimat.
        /// </summary>
        private static string BersihkanPenandaCatatanKaki(string teks)
        {
            if (string.IsNullOrEmpty(teks)) return teks;

            teks = Regex.Replace(teks, @"(?<=[^\s\d])(\d+\))", " $1");
            teks = Regex.Replace(teks, @"(\d+)\)", m => KeSuperscript(m.Groups[1].Value) + "\u207E");

            return teks;
        }

        private static string KeSuperscript(string angka)
        {
            var peta = new Dictionary<char, char>
            {
                {'0', '\u2070'}, {'1', '\u00B9'}, {'2', '\u00B2'}, {'3', '\u00B3'}, {'4', '\u2074'},
                {'5', '\u2075'}, {'6', '\u2076'}, {'7', '\u2077'}, {'8', '\u2078'}, {'9', '\u2079'},
            };
            var sb = new System.Text.StringBuilder();
            foreach (var c in angka)
            {
                sb.Append(peta.TryGetValue(c, out var sup) ? sup : c);
            }
            return sb.ToString();
        }

        

        public static async Task<List<Juz>> AmbilDaftarJuzAsync()
        {
            var daftar = new List<Juz>();

            using var koneksi = new SqliteConnection(ConnectionString);
            await koneksi.OpenAsync();

            var cmd = koneksi.CreateCommand();
            cmd.CommandText = """
                SELECT juz, MIN(surah) as surahMulai, MIN(ayat) as ayatMulai
                FROM (
                    SELECT juz, surah, ayat,
                           ROW_NUMBER() OVER (PARTITION BY juz ORDER BY id) as rn
                    FROM quran
                )
                WHERE rn = 1
                GROUP BY juz
                ORDER BY juz
                """;

            using var reader = await cmd.ExecuteReaderAsync();
            var sementara = new List<(int juz, int surah, int ayat)>();
            while (await reader.ReadAsync())
            {
                sementara.Add((reader.GetInt32(0), reader.GetInt32(1), reader.GetInt32(2)));
            }

            var daftarSurah = await AmbilDaftarSurahAsync();

            for (int i = 0; i < sementara.Count; i++)
            {
                var (juz, surahMulai, ayatMulai) = sementara[i];
                int surahSelesai, ayatSelesai;

                if (i + 1 < sementara.Count)
                {
                    var (_, surahBerikut, ayatBerikut) = sementara[i + 1];
                    if (ayatBerikut > 1)
                    {
                        surahSelesai = surahBerikut;
                        ayatSelesai = ayatBerikut - 1;
                    }
                    else
                    {
                        surahSelesai = surahBerikut - 1;
                        var surahInfo = daftarSurah.Find(s => s.Nomor == surahSelesai);
                        ayatSelesai = surahInfo?.JumlahAyat ?? ayatBerikut;
                    }
                }
                else
                {
                    surahSelesai = 114;
                    ayatSelesai = 6;
                }

                string namaAwal = daftarSurah.Find(s => s.Nomor == surahMulai)?.NamaLatin ?? "";
                string namaAkhir = daftarSurah.Find(s => s.Nomor == surahSelesai)?.NamaLatin ?? "";

                string rentang = surahMulai == surahSelesai
                    ? $"{namaAwal} ({ayatMulai}-{ayatSelesai})"
                    : $"{namaAwal} ({ayatMulai}) - {namaAkhir} ({ayatSelesai})";

                daftar.Add(new Juz
                {
                    Nomor = juz,
                    Rentang = rentang,
                    SurahMulai = surahMulai,
                    AyatMulai = ayatMulai,
                    SurahSelesai = surahSelesai,
                    AyatSelesai = ayatSelesai,
                });
            }

            return daftar;
        }
    }
}

