using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using QuranKemenag.Models;
using Windows.Storage;
using System.Linq;

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
            if (File.Exists(DbPath)) return; // sudah pernah disalin sebelumnya

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
            cmd.CommandText = "SELECT ayat, teks FROM quran WHERE surah = $nomor ORDER BY ayat";
            cmd.Parameters.AddWithValue("$nomor", nomorSurah);

            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                daftar.Add(new Ayat
                {
                    NomorSurah = nomorSurah,
                    NomorAyat = reader.GetInt32(0),
                    TeksArab = reader.GetString(1),
                    TeksLatin = "",       // belum tersedia di database ini
                    Terjemah = "",        // tempat siap diisi saat sumber terjemahan tersedia
                    CatatanKaki = null,
                });
            }

            return daftar;
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

