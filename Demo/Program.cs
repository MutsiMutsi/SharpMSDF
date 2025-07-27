using System;
using System.CodeDom.Compiler;
using System.IO;
using Typography.OpenFont;
using SharpMSDF.IO;
using SharpMSDF.Core;
using System.Runtime.CompilerServices;
using OpMode = SharpMSDF.Core.ErrorCorrectionConfig.OpMode;
using ConfigDistanceCheckMode = SharpMSDF.Core.ErrorCorrectionConfig.ConfigDistanceCheckMode;

namespace SharpMSDF.Demo
{
    internal static class Program
    {
        static void Main(string[] args)
        {
            var font = FontImporter.LoadFont("consolab.ttf"); 
            var shape = FontImporter.LoadGlyph(font, 'A', FontCoordinateScaling.EmNormalized);
            int size = 64;
            var msdf = new Bitmap<float>( size, size, 3);

            shape.OrientContours(); // This will orient the windings
            shape.Normalize();
            EdgeColoring.EdgeColoringSimple(shape, 3.0); // Angle Thereshold

            var distMap = new DistanceMapping(new(4f/size)); // Range
                                                                      //  Scale    Translation  
            var transformation = new SDFTransformation( new Projection( new(size-2), new(0.125)), distMap);

            MSDFGen.GenerateMSDF(
                msdf,
                shape,
                transformation,
                new MSDFGeneratorConfig(
                    overlapSupport: true, 
                    errorCorrection: new ErrorCorrectionConfig(OpMode.EDGE_PRIORITY, ConfigDistanceCheckMode.ALWAYS_CHECK_DISTANCE)
                )
            );
            Png.SavePng(msdf, "output.png");
            
            // Rendering Preview
            var rast = new Bitmap<float>(1024, 1024);
            Render.RenderSdf(rast, msdf, 8.0);
            Png.SavePng(rast, "rasterized.png");
        }
    }
}
