using System;
using System.CodeDom.Compiler;
using Msdfgen.IO;
using Typography.OpenFont;

namespace Msdfgen.ManualTest
{
    internal static class Program
    {
        static void Main(string[] args)
        {
            double advance = 0;
            var font = ImportFont.LoadFont("micross.ttf"); 
            var shape = ImportFont.LoadGlyph(font, 'A', ref advance, out int bitmapWidth, out int bitmapHeight);
            var msdf = new Bitmap<FloatRgb>(2*bitmapWidth, 2*bitmapHeight);
            var generator = Generate.Msdf();
            generator.Output = msdf;
            generator.Range = 3.0;
            generator.EdgeThreshold = 3.0;
            generator.Scale = new Vector2(2.0);

            for (int i = 0; i < 5; ++i)
            {
                shape.Normalize();
                Coloring.EdgeColoringSimple(shape, 3.0);
                generator.Shape = shape;
                generator.Compute();
                if (i % 100 == 0)
                    Console.WriteLine(i);
            }

            Bmp.SaveBmp(msdf, "output.bmp");
            {
                // MDSF Text
                var rast = new Bitmap<float>(32, 32);
                Render.RenderSdf(rast, msdf, 6.0);
                Bmp.SaveBmp(rast, "rasterized.bmp");
            }
            ImportFont.DestroyFont(font);
        }
    }
}
