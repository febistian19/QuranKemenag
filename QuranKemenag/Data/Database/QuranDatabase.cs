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
        private static string DbPathUtsmani => Path.Combine(ApplicationData.Current.LocalFolder.Path, "msi_simbol.db");

        private static string ConnectionString => $"Data Source={DbPath}";
        private static string ConnectionStringUtsmani => $"Data Source={DbPathUtsmani}";

        /// <summary>Nomor surah yang datanya sudah tersedia di database Utsmani (msi_simbol.db) — masih data percobaan.</summary>
        public static int[] SurahTersediaUtsmani { get; private set; } = { 1, 2 };

        /// <summary>
        /// Dipanggil sekali saat aplikasi start. Menyalin database bawaan (Asset)
        /// ke folder lokal aplikasi, hanya jika belum pernah disalin sebelumnya.
        /// </summary>
        public static async Task InisialisasiAsync()
        {
            var targetFolder = ApplicationData.Current.LocalFolder;

            var assetFile = await StorageFile.GetFileFromApplicationUriAsync(
                new Uri("ms-appx:///Assets/quran.db"));
            await assetFile.CopyAsync(targetFolder, "quran.db", NameCollisionOption.ReplaceExisting);

            var assetFileUtsmani = await StorageFile.GetFileFromApplicationUriAsync(
                new Uri("ms-appx:///Assets/msi_simbol.db"));
            await assetFileUtsmani.CopyAsync(targetFolder, "msi_simbol.db", NameCollisionOption.ReplaceExisting);

            await MuatSurahTersediaUtsmaniAsync();
        }

        /// <summary>Baca daftar surah yang sudah ada datanya di msi_simbol.db (otomatis ikut bertambah saat data ditambah).</summary>
        private static async Task MuatSurahTersediaUtsmaniAsync()
        {
            try
            {
                using var koneksi = new SqliteConnection(ConnectionStringUtsmani);
                await koneksi.OpenAsync();
                var cmd = koneksi.CreateCommand();
                cmd.CommandText = "SELECT DISTINCT surah FROM quran ORDER BY surah";
                var hasil = new List<int>();
                using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync()) hasil.Add(reader.GetInt32(0));
                if (hasil.Count > 0) SurahTersediaUtsmani = hasil.ToArray();
            }
            catch { }
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
                SELECT q.ayat, q.isyarat, t.teks AS terjemah, t.footnote, q.juz
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
                    TeksLatin = "",
                    Terjemah = BersihkanPenandaCatatanKaki(reader.IsDBNull(2) ? "" : reader.GetString(2)),
                    CatatanKaki = string.IsNullOrWhiteSpace(footnote) ? null : footnote,
                    Juz = reader.GetInt32(2),
                });
            }

            return daftar;
        }

        /// <summary>
        /// Ambil ayat dari database khusus font Utsmani (msi_simbol.db) — teksnya pakai encoding
        /// PUA (Private Use Area), cuma bisa dirender benar dengan font "Lpmq_Hafs_01". Data di
        /// database ini masih percobaan, baru mencakup surah yang ada di SurahTersediaUtsmani.
        /// Terjemahan tetap diambil dari quran.db utama (terjemah_id) supaya tidak perlu duplikasi data.
        /// </summary>
        public static async Task<List<Ayat>> AmbilAyatSurahUtsmaniAsync(int nomorSurah)
        {
            var daftar = new List<Ayat>();

            using (var koneksiUtsmani = new SqliteConnection(ConnectionStringUtsmani))
            {
                await koneksiUtsmani.OpenAsync();

                var cmd = koneksiUtsmani.CreateCommand();
                cmd.CommandText = "SELECT ayat, teks, juz, kode_font FROM quran WHERE surah = $nomor ORDER BY ayat";
                cmd.Parameters.AddWithValue("$nomor", nomorSurah);

                using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    // Teks disimpan dalam urutan ASLI (tidak dibalik manual). Dibungkus karakter
                    // kontrol Unicode RLO (\u202E) ... PDF (\u202C) supaya mesin render Windows
                    // memaksa urutan tampil kanan-ke-kiri yang benar (per kata & per baris saat wrap),
                    // walau kode PUA di font ini secara default tidak dikenali sebagai "huruf RTL".
                    string teksUtsmani = HapusSimbolAkhirAyat(reader.GetString(1));
                    string teksRtl = "\u202E" + teksUtsmani + "\u202C";

                    daftar.Add(new Ayat
                    {
                        NomorSurah = nomorSurah,
                        NomorAyat = reader.GetInt32(0),
                        TeksArab = teksRtl,
                        TeksLatin = "",
                        Juz = reader.GetInt32(2),

                        // kode_font = nomor file Lpmq_Hafs_NN; kalau kosong dianggap font 1
                        KodeFont = reader.IsDBNull(3) ? 1 : reader.GetInt32(3),
                    });
                }
            }

            if (daftar.Count == 0) return daftar;

            // Lengkapi terjemahan & catatan kaki dari quran.db utama (kalau ada).
            using (var koneksiUtama = new SqliteConnection(ConnectionString))
            {
                await koneksiUtama.OpenAsync();

                var cmdTerjemah = koneksiUtama.CreateCommand();
                cmdTerjemah.CommandText = "SELECT ayat, teks, footnote FROM terjemah_id WHERE surah = $nomor";
                cmdTerjemah.Parameters.AddWithValue("$nomor", nomorSurah);

                var petaTerjemah = new Dictionary<int, (string Teks, string? Footnote)>();
                using (var readerT = await cmdTerjemah.ExecuteReaderAsync())
                {
                    while (await readerT.ReadAsync())
                    {
                        petaTerjemah[readerT.GetInt32(0)] = (
                            readerT.IsDBNull(1) ? "" : readerT.GetString(1),
                            readerT.IsDBNull(2) ? null : readerT.GetString(2));
                    }
                }

                foreach (var ayat in daftar)
                {
                    if (petaTerjemah.TryGetValue(ayat.NomorAyat, out var t))
                    {
                        ayat.Terjemah = BersihkanPenandaCatatanKaki(t.Teks);
                        ayat.CatatanKaki = string.IsNullOrWhiteSpace(t.Footnote) ? null : t.Footnote;
                    }
                }
            }

            return daftar;
        }

        private static string HapusSimbolAkhirAyat(string teks)
        {
            teks = teks.TrimEnd();

            if (teks.Length > 0)
            {
                char karakterTerakhir = teks[^1];
                if (karakterTerakhir is >= '\uE000' and <= '\uF8FF')
                {
                    return teks[..^1];
                }
            }

            return teks;
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

