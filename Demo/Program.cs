using System;
using System.CodeDom.Compiler;
using System.IO;
using Typography.OpenFont;
using SharpMSDF.IO;
using SharpMSDF.Core;
using System.Runtime.CompilerServices;
using OpMode = SharpMSDF.Core.ErrorCorrectionConfig.OpMode;
using ConfigDistanceCheckMode = SharpMSDF.Core.ErrorCorrectionConfig.ConfigDistanceCheckMode;
using SharpMSDF.Atlas;
using System.Reflection.Emit;
using System.Collections.Generic;
using System.Drawing;
using System.Buffers;

namespace SharpMSDF.Demo
{
    internal static class Program
    {
        static void Main(string[] args)
        {
            var font = FontImporter.LoadFont("Kingthings_Petrock.ttf");
            OneGlyphGen(font);
            ImediateAtlasGen(font);
            OnDemandAtlasGen(font);
        }

        private static void ImediateAtlasGen(Typeface font)
        {
            List<GlyphGeometry> glyphs = new (font.GlyphCount);
            // FontGeometry is a helper class that loads a set of glyphs from a single font.
            // It can also be used to get additional font metrics, kerning information, etc.
            FontGeometry fontGeometry = new (glyphs);
            // Load a set of character glyphs:
            // The second argument can be ignored unless you mix different font sizes in one atlas.
            // In the last argument, you can specify a charset other than ASCII.
            // To load specific glyph indices, use loadGlyphs instead.
            fontGeometry.LoadCharset(font, 1.0, Charset.ASCII);
            // Apply MSDF edge coloring. See edge-coloring.h for other coloring strategies.
            const double maxCornerAngle = 3.0;
            for (var g = 0; g < glyphs.Count; g++)
            {
                glyphs[g].GetShape().OrientContours();
                glyphs[g].EdgeColoring(EdgeColorings.InkTrap, maxCornerAngle, 0);
            }
            // TightAtlasPacker class computes the layout of the atlas.
            TightAtlasPacker packer = new();
            // Set atlas parameters:
            // setDimensions or setDimensionsConstraint to find the best value
            packer.SetDimensionsConstraint(DimensionsConstraint.Square);
            // setScale for a fixed size or setMinimumScale to use the largest that fits
            packer.SetMinimumScale(64.0);
            // setPixelRange or setUnitRange
            packer.SetPixelRange(new DoubleRange(6.0));
            packer.SetMiterLimit(1.0);
            packer.SetOriginPixelAlignment(false, true);
            // Compute atlas layout - pack glyphs
            packer.Pack(ref glyphs);
            // Get final atlas dimensions
            packer.GetDimensions(out int width, out int height);

            //Gen function
            GeneratorFunction<float> msdfGen = GlyphGenerators.Msdf;
            
            // The ImmediateAtlasGenerator class facilitates the generation of the atlas bitmap.
            ImmediateAtlasGenerator <
                    float, // pixel type of buffer for individual glyphs depends on generator function
                    BitmapAtlasStorage<byte> // class that stores the atlas bitmap
                    // For example, a custom atlas storage class that stores it in VRAM can be used.
                > generator = new(width, height, 3, msdfGen);
            // GeneratorAttributes can be modified to change the generator's default settings.
            GeneratorAttributes attributes = new();
            generator.SetAttributes(attributes);
            generator.SetThreadCount(4);
            // Generate atlas bitmap
            generator.Generate(glyphs);
            // The atlas bitmap can now be retrieved via atlasStorage as a BitmapConstRef.
            // The glyphs array (or fontGeometry) contains positioning data for typesetting text.

            Png.SavePng(generator.Storage.Bitmap, "atlas.png");
        }

        private static void OnDemandAtlasGen(Typeface font)
        {
            const double pixelRange = 6.0;
            const double glyphScale = 64.0;
            const double miterLimit = 2.0;
            const double maxCornerAngle = 3.0;

            List<GlyphGeometry> glyphs = new(font.GlyphCount);
            FontGeometry fontGeometry = new(glyphs);

            DynamicAtlas<ImmediateAtlasGenerator<float, BitmapAtlasStorage<byte>>, TightAtlasPacker> myDynamicAtlas = new();
            myDynamicAtlas.Generator = new(3, GlyphGenerators.Msdf);
            myDynamicAtlas.Packer = new();

            Console.WriteLine("Dynamic Atlas Generator Demo");
            while (true)
            {
                Console.WriteLine("Enter char(s) to be added to Atlas (Make sure each chars are different):");
                ReadOnlySpan<char> chars = Console.ReadLine();

                Charset charset = new Charset();

                int prevEndMark = glyphs.Count;

                for (int c = 0; c < chars.Length; ++c)
                    charset.Add(chars[c]);
                fontGeometry.LoadCharset(font, 1.0, charset);

                for (int g = prevEndMark; g <  glyphs.Count; ++g)
                { 
                    var glyph = glyphs[g];
                    // Preprocess windings
                    glyph.GetShape().OrientContours();
                    // Apply MSDF edge coloring. See edge-coloring.h for other coloring strategies.
                    glyph.EdgeColoring(EdgeColorings.InkTrap, maxCornerAngle, 0);
                    // Finalize glyph box size based on the parameters
                    glyph.WrapBox(ref glyph, new() { Scale = glyphScale, Range = new( pixelRange / glyphScale), MiterLimit = miterLimit });
                    
                    glyphs[g] = glyph;
                }

                var changeFlags = myDynamicAtlas.Add(glyphs[prevEndMark..]);

                var bitmap = myDynamicAtlas.Generator.Storage.Bitmap;

                Png.SavePng(bitmap, "dynamic-atlas.png");
            }
        }

        private static void OneGlyphGen(Typeface font)
        {
            var shape = FontImporter.LoadGlyph(font, '#', FontCoordinateScaling.EmNormalized);
            int size = 64;
            var msdf = new Bitmap<float>(size, size, 3);

            shape.OrientContours(); // This will orient the windings
            shape.Normalize();

            EdgeColorings.InkTrap(shape, 3.0); // Angle Thereshold

            double l=0.0, b = 0.0, r = 0.0, t = 0.0;
            shape.Bound(ref l, ref b, ref r, ref t);
            var minSize = size * Math.Min(t - b, r - l);
            var maxSize = size * Math.Max(t - b, r - l);
            double pxrange = 6.0;
            var distMap = new DistanceMapping(new(-pxrange / size, pxrange / size)); // Range
                                                                   //  Scale    Translation  
            var transformation = new SDFTransformation(new Projection(new(size), new(0)), distMap);


            MSDFGen.GenerateMSDF(
                msdf,
                shape,
                transformation,
                new MSDFGeneratorConfig(
                    overlapSupport: false,
                    errorCorrection: new ErrorCorrectionConfig(OpMode.EDGE_PRIORITY, ConfigDistanceCheckMode.CHECK_DISTANCE_AT_EDGE)
                )
            );
            //Rasterization.DistanceSignCorrection(msdf, shape, transformation.Projection, FillRule.FILL_NONZERO);
            //MSDFErrorCorrection.ErrorCorrection(msdf, shape, transformation, new MSDFGeneratorConfig(
            //    overlapSupport: false,
            //    errorCorrection: new ErrorCorrectionConfig(OpMode.DISABLED, ConfigDistanceCheckMode.DO_NOT_CHECK_DISTANCE)
            //    )
            //);


            Png.SavePng(msdf, "output.png");

            // Rendering Preview
            var rast = new Bitmap<float>(1024, 1024);
            Render.RenderSdf(rast, msdf, pxrange);
            Png.SavePng(rast, "rasterized.png");
        }
    
    }
}
