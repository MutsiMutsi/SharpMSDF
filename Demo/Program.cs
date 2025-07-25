using System;
using System.CodeDom.Compiler;
using System.IO;
using Typography.OpenFont;
using SharpMSDF.IO;
using SharpMSDF.Core;
using System.Runtime.CompilerServices;

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
            var msdf = new Bitmap<float>( scale*bitmapWidth, scale*bitmapHeight, 3);

            shape.Normalize();
            EdgeColoring.EdgeColoringSimple(shape, 3.0); // Angle Thereshold

            var distMap = new DistanceMapping(new(1.0)); // Range
            var transformation = new SDFTransformation( new Projection( new(scale), new(0)), distMap);

            MSDFGen.GenerateMSDF(msdf, shape, transformation);
            Bmp.SaveBmp(msdf, "output.bmp");
            
            // Rendering Preview
            var rast = new Bitmap<float>(1024, 1024);
            Render.RenderSdf(rast, msdf, 12.0);
            Bmp.SaveBmp(rast, "rasterized.bmp");
        }
    }
}
