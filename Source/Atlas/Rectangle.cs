using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharpMSDF.Atlas
{
    public struct Rectangle(int x, int y, int w, int h)
    {
        public int X = x, Y = y, Width = w, Height = h;
    };

    public struct OrientedRectangle
    {
        public int X, Y, Width, Height;
        public bool Rotated;

        public static implicit operator Rectangle(OrientedRectangle rect) => new Rectangle(rect.X, rect.Y, rect.Width, rect.Height);
    };
}
