using System.Drawing;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Security.Cryptography.X509Certificates;
using Typography.OpenFont.Tables;

namespace SharpMSDF.Core
{
    //public struct FloatRgb
    //{
    //    public float R, G, B;
    //    public static implicit operator FloatRgb((float r, float g, float b) col)
    //    {
    //        return new FloatRgb() { R = col.r, G = col.g, B = col.b };
    //    }
    //    public static implicit operator FloatRgb(float col)
    //    {
    //        return new FloatRgb() { R = col, G = col, B = col };
    //    }
    //}

    public class Bitmap
    {
        public float[] Pixels;
        public int N;

        internal Bitmap() { }
        public Bitmap(int width, int height, int channels = 1)
        {
            _Width = width;
            _Height = height;
            N = channels;
            Pixels = new float[channels * width * height];
        }

        public ref float this[int x, int y] => ref Pixels[N * (Width * y + x)];

        internal int _Width;
        public int Width => _Width;

        internal int _Height;
        public int Height => _Height; 

        public static void Interpolate(Span<float> output, BitmapConstRef bitmap, Vector2 pos)
        {
            pos -= new Vector2(.5);
            int l = (int)Math.Floor(pos.X);
            int b = (int)Math.Floor(pos.Y);
            int r = l + 1;
            int t = b + 1;
            double lr = pos.X - l;
            double bt = pos.Y - b;
            l = Arithmetic.Clamp(l, bitmap.SubWidth - 1); r = Arithmetic.Clamp(r, bitmap.SubWidth - 1);
            b = Arithmetic.Clamp(b, bitmap.SubHeight - 1); t = Arithmetic.Clamp(t, bitmap.SubHeight - 1);
            for (int i = 0; i < bitmap.N; ++i)
                //...
                output[i] = Arithmetic.Mix(Arithmetic.Mix(bitmap[l, b, i], bitmap[r, b, i], lr), Arithmetic.Mix(bitmap[l, t, i], bitmap[r, t, i], lr), bt);
        }
    }
}