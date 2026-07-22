using QuranKemenag.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace QuranKemenag.Services
{
    public static class BookmarkService
    {
        private static readonly string _folder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "QuranKemenagDesktop");

        private static readonly string _filePath = Path.Combine(_folder, "bookmarks.json");

        private static List<Bookmark> _cache = new();
        private static bool _loaded = false;

        private static void PastikanFolderAda()
        {
            if (!Directory.Exists(_folder))
                Directory.CreateDirectory(_folder);
        }

        private static void MuatDariFile()
        {
            if (_loaded) return;

            PastikanFolderAda();

            if (File.Exists(_filePath))
            {
                try
                {
                    var json = File.ReadAllText(_filePath);
                    _cache = JsonSerializer.Deserialize<List<Bookmark>>(json) ?? new List<Bookmark>();
                }
                catch
                {
                    _cache = new List<Bookmark>();
                }
            }

            _loaded = true;
        }

        private static void SimpanKeFile()
        {
            PastikanFolderAda();
            var json = JsonSerializer.Serialize(_cache, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(_filePath, json);
        }

        public static List<Bookmark> AmbilSemua()
        {
            MuatDariFile();
            return _cache.OrderByDescending(b => b.DisimpanPada).ToList();
        }

        public static bool SudahDisimpan(int nomorSurah, int nomorAyat)
        {
            MuatDariFile();
            return _cache.Any(b => b.NomorSurah == nomorSurah && b.NomorAyat == nomorAyat);
        }

        public static void Simpan(int nomorSurah, int nomorAyat, string namaSurah)
        {
            MuatDariFile();
            if (SudahDisimpan(nomorSurah, nomorAyat)) return;

            _cache.Add(new Bookmark
            {
                NomorSurah = nomorSurah,
                NomorAyat = nomorAyat,
                NamaSurah = namaSurah
            });
            SimpanKeFile();
        }

        public static void Hapus(int nomorSurah, int nomorAyat)
        {
            MuatDariFile();
            _cache.RemoveAll(b => b.NomorSurah == nomorSurah && b.NomorAyat == nomorAyat);
            SimpanKeFile();
        }

        public static void Toggle(int nomorSurah, int nomorAyat, string namaSurah)
        {
            if (SudahDisimpan(nomorSurah, nomorAyat))
                Hapus(nomorSurah, nomorAyat);
            else
                Simpan(nomorSurah, nomorAyat, namaSurah);
        }
    }
}