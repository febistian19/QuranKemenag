using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;


namespace QuranKemenag.Models
{
    public class Surah
    {
    public int Nomor { get; set; }
    public string NamaLatin { get; set; } = string.Empty;
    public string NamaArab { get; set; } = string.Empty;
    public string Arti { get; set; } = string.Empty;
    public int JumlahAyat { get; set; }
    public string TempatTurun { get; set; } = string.Empty; // "Makkiyah" / "Madaniyah"

    public string DisplayJudul => $"{NamaLatin} ({Arti})";
    public string DisplayInfo => $"{JumlahAyat} ayat | {TempatTurun}";
    }
}