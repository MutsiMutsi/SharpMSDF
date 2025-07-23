using System;
using System.CodeDom.Compiler;
using System.IO;
using Msdfgen.IO;
using SharpMSDF.IO;
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
            int scale = 2;
            var msdf = new Bitmap<FloatRgb>(scale * bitmapWidth, scale * bitmapHeight);

            var generator = Generate.Msdf();
            generator.Output = msdf;
            generator.Range = 0.5;
            generator.EdgeThreshold = 0.0;
            generator.Scale = new Vector2(scale);

            shape.Normalize();
            Coloring.EdgeColoringSimple(shape, 1.0);
            generator.Shape = shape;
            generator.Compute();

            // MSDF
            Bmp.SaveBmp(msdf, "output.bmp");
            // Rendering Preview
            var rast = new Bitmap<float>(1024, 1024);
            Render.RenderSdf(rast, msdf, 6.0);
            Bmp.SaveBmp(rast, "rasterized.bmp");
        }
    }
}
