using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharpMSDF.Atlas
{

    /// Represents a set of Unicode codepoints (characters)
    class Charset
    {

        /// The set of the 95 printable ASCII characters
        public readonly static Charset ASCII = CreateAsciiCharset();

        static Charset CreateAsciiCharset()
        {
            Charset ascii = new();
            for (uint cp = 0x20; cp < 0x7f; ++cp)
                ascii.Add(cp);
            return ascii;
        }

        /// Adds a codepoint
        public void Add(uint cp) => _Codepoints.Add(cp);
        /// Removes a codepoint
        public void Remove(uint cp) => _Codepoints.Remove(cp);

        public int Size() => _Codepoints.Count;
        public bool Empty() => _Codepoints.Count == 0;

        //std::set<unicode_t>::const_iterator begin()
        //{

        //}
        //std::set<unicode_t>::const_iterator end()
        //{

        //}

        /// Load character set from a text file with compliant syntax
        public bool Load(string filename, bool disableCharLiterals = false)
        {
            
        }
        /// Parse character set from a string with compliant syntax
        public bool Parse(string str, int strLength, bool disableCharLiterals = false)
        {

        }

        private SortedSet<uint> _Codepoints;

    };

}

}
