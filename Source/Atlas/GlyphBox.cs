using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharpMSDF.Atlas
{
    public struct Bounds
    {
        public double L, B, R, T;
    }

    /// The glyph box - its bounds in plane and atlas
    public struct GlyphBox
    {
        public int Index;
        public double Advance;
        public Bounds Bounds;   
        public Rectangle Rect;

    }

}
