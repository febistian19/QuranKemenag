using System;

namespace QuranKemenag.Models
{
    /// <summary>
    /// Represents available modes for rendering Quran text.
    /// This setting decides which underlying database / text source will be used
    /// in future stages of the application (e.g. Unicode or Symbol database).
    /// </summary>
    public enum QuranTextMode
    {
        /// <summary>
        /// Use the Unicode database / source. This is the default mode.
        /// </summary>
        Unicode = 0,

        /// <summary>
        /// Use the Symbol database / source. (Not yet implemented in database access.)
        /// </summary>
        Symbol = 1,
    }
}
