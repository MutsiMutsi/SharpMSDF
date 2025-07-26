using System;
using System.CodeDom.Compiler;
using System.IO;
using Typography.OpenFont;
using SharpMSDF.IO;
using SharpMSDF.Core;
using System.Runtime.CompilerServices;

namespace SharpMSDF.Demo
{
    internal static class Program
    {
        static void Main(string[] args)
        {
            double advance = 0;
            var font = FontImporter.LoadFont("micross.ttf"); 
            var shape = FontImporter.LoadGlyphShape(font, 'A', ref advance, out int bitmapWidth, out int bitmapHeight);
            int scale = 3;
            var msdf = new Bitmap<float>( scale*bitmapWidth, scale*bitmapHeight, 3);

            shape.Normalize();
            EdgeColoring.EdgeColoringSimple(shape, 3.0); // Angle Thereshold

            var distMap = new DistanceMapping(new(1.5)); // Range
            var transformation = new SDFTransformation( new Projection( new(scale), new()), distMap);

            MSDFGen.GenerateMSDF(
                msdf,
                shape,
                transformation,
                new MSDFGeneratorConfig(true, new ErrorCorrectionConfig(ErrorCorrectionConfig.OpMode.EDGE_PRIORITY, ErrorCorrectionConfig.ConfigDistanceCheckMode.ALWAYS_CHECK_DISTANCE))
            );
            Bmp.SaveBmp(msdf, "output.bmp");
            
            // Rendering Preview
            var rast = new Bitmap<float>(1024, 1024);
            Render.RenderSdf(rast, msdf, 3.0);
            Bmp.SaveBmp(rast, "rasterized.bmp");
        }
    }
}
