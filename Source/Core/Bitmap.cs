using System.Drawing;
using Typography.OpenFont.Tables;

namespace SharpMSDF.Core
{
    public struct FloatRgb
    {
        public float R, G, B;
        public static implicit operator FloatRgb((float r, float g, float b) col)
        {
            return new FloatRgb() { R = col.r, G = col.g, B = col.b };
        }
        public static implicit operator FloatRgb(float col)
        {
            return new FloatRgb() { R = col, G = col, B = col };
        }
    }

    public class Bitmap<T> where T : struct
    {
        public readonly T[] Pixels;
        public int N;

        public Bitmap(int width, int height, int channels=1)
        {
            Width = width;
            Height = height;
            N = channels;
            Pixels = new T[channels*width*height];
        }

        public ref T this[int x, int y] => ref Pixels[N * (Width * y + x)];

        public int Width { get; }

        public int Height { get; }
    }
}