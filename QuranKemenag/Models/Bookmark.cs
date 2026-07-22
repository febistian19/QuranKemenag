using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuranKemenag.Models
{
    public class Bookmark
    {
        public int NomorSurah { get; set; }
        public int NomorAyat { get; set; }
        public string NamaSurah { get; set; } = string.Empty;
        public DateTime DisimpanPada { get; set; } = DateTime.Now;
    }
}