using System;
using System.CodeDom.Compiler;
using System.IO;
using Typography.OpenFont;
using SharpMSDF.IO;
using SharpMSDF.Core;

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
            var msdf = new Bitmap<float>(scale * bitmapWidth, scale * bitmapHeight, 1);

            var transformation = new SDFTransformation() { DistanceMapping = new(new(6.0)), Projection = new(new(scale), new(0.0)) };

            var msdfRef = new BitmapRef<float>(msdf.Pixels, bitmapWidth, bitmapHeight, 1);
            MSDFGen.GenerateSDF(msdfRef, shape, transformation);            

            // MSDF
            Bmp.SaveBmp(msdfRef, "output.bmp");
            // Rendering Preview
            var rast = new Bitmap<float>(1024, 1024);
            //Render.RenderSdf(rast, msdf, 6.0);
            //Bmp.SaveBmp(rast, "rasterized.bmp");
        }
    }
}
