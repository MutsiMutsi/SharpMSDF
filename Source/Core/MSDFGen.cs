using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
// Equivalent to: typedef BitmapRef<float, 1>
using BitmapRefSingle = SharpMSDF.Core.BitmapRef<float>;
// Equivalent to: typedef BitmapRef<float, 3>
using BitmapRefMulti = SharpMSDF.Core.BitmapRef<float>;
// Equivalent to: typedef BitmapRef<float, 4>
using BitmapRefMultiAndTrue = SharpMSDF.Core.BitmapRef<float>;
using Typography.OpenFont;
using Typography.OpenFont.Tables;


namespace SharpMSDF.Core
{
    public static class MSDFGen
    {
        public unsafe abstract class DistancePixelConversion<TDistance>
        {
            public DistanceMapping Mapping;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public abstract void Convert(float* pixels, TDistance distance);
        }

        public unsafe class DistancePixelConversionSingle : DistancePixelConversion<double>
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public override void Convert(float* pixels, double distance)
            {
                pixels[0] = (float)Mapping[distance];
            }
        }

        public unsafe class DistancePixelConversionMulti : DistancePixelConversion<MultiDistance>
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public override void Convert(float* pixels, MultiDistance distance)
            {
                pixels[0] = (float)Mapping[distance.R];
                pixels[1] = (float)Mapping[distance.G];
                pixels[2] = (float)Mapping[distance.B];
            }
        }

        public unsafe class DistancePixelConversionMultiAndTrue : DistancePixelConversion<MultiAndTrueDistance>
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public override void Convert(float* pixels, MultiAndTrueDistance distance)
            {
                pixels[0] = (float)Mapping[distance.R];
                pixels[1] = (float)Mapping[distance.G];
                pixels[2] = (float)Mapping[distance.B];
                pixels[3] = (float)Mapping[distance.A];
            }
        }

        /// Generates a conventional single-channel signed distance field.
        public unsafe static void GenerateDistanceField<TCombiner, TConverter, TDistanceSelector, TDistance>(BitmapRefSingle output, Shape shape, SDFTransformation transformation) 
            where TDistanceSelector : IDistanceSelector<TDistance>, new() 
            where TCombiner : ContourCombiner<TDistanceSelector, TDistance>, new()
            where TConverter : DistancePixelConversion<TDistance>, new()
        {
            // 1. Create the converter
            var converter = new TConverter() { Mapping = transformation.DistanceMapping };
            // 2. Create your combiner‐driven distance finder
            var distanceFinder = new ShapeDistanceFinder<TCombiner,TDistanceSelector, TDistance>(shape);

            // 3. Parallel loop over rows
            bool rightToLeft = false;

            Parallel.For(0, output.Height, y =>
            {
                int row = shape.InverseYAxis ? output.Height - y - 1 : y;
                for (int col = 0; col < output.Width; col++)
                {
                    int x = rightToLeft ? output.Width - col - 1 : col;
                    // unproject into Shape‐space
                    var p = transformation.Projection.UnprojectVector(new Vector2(x + .5f, y + .5f));
                    // get the signed‐distance
                    TDistance dist = distanceFinder.Distance(p);
                    // write into the pixel buffer
                    float* pixel = output.Pixels + output.N * (output.Width * row + x);
                    converter.Convert(pixel, dist); 
                }
                rightToLeft = !rightToLeft; // flip for “staggered” ordering
            });
        }


        public static void GenerateSDF(BitmapRefSingle output, Shape shape, SDFTransformation transformation, GeneratorConfig config = default)
        {
            if (config.OverlapSupport)
                GenerateDistanceField<OverlappingContourCombiner<TrueDistanceSelector, double>, DistancePixelConversionSingle, TrueDistanceSelector, double>
                    (output, shape, transformation);
            else
                GenerateDistanceField<SimpleContourCombiner<TrueDistanceSelector, double>, DistancePixelConversionSingle, TrueDistanceSelector, double>
                    (output, shape, transformation);
        }

        /// Generates a single-channel signed perpendicular distance field.
        public static void GeneratePSDF(BitmapRefSingle output, Shape shape, SDFTransformation transformation, GeneratorConfig config = default)
        {
            if (config.OverlapSupport)
                GenerateDistanceField<OverlappingContourCombiner<PerpendicularDistanceSelector, double>, DistancePixelConversionSingle, PerpendicularDistanceSelector, double>
                    (output, shape, transformation);
            else
                GenerateDistanceField<SimpleContourCombiner<PerpendicularDistanceSelector, double>, DistancePixelConversionSingle, PerpendicularDistanceSelector, double>
                    (output, shape, transformation);
        }

        /// Generates a multi-channel signed distance field. Edge colors must be assigned first! (See edgeColoringSimple)
        public static void GenerateMSDF(BitmapRefMulti output, Shape shape, SDFTransformation transformation, MSDFGeneratorConfig config = default)
        {
            if (config.OverlapSupport)
                GenerateDistanceField<OverlappingContourCombiner<MultiDistanceSelector, MultiDistance>, DistancePixelConversionMulti, MultiDistanceSelector, MultiDistance>
                    (output, shape, transformation);
            else
                GenerateDistanceField<SimpleContourCombiner<MultiDistanceSelector, MultiDistance>, DistancePixelConversionMulti, MultiDistanceSelector, MultiDistance>
                    (output, shape, transformation);
            // TODO : Error Correction method
        }

        /// Generates a multi-channel signed distance field with true distance in the alpha channel. Edge colors must be assigned first.
        public static void GenerateMTSDF(BitmapRefMultiAndTrue output, Shape shape, SDFTransformation transformation, MSDFGeneratorConfig config = default)
        {
            if (config.OverlapSupport)
                GenerateDistanceField<OverlappingContourCombiner<MultiAndTrueDistanceSelector, MultiAndTrueDistance>, DistancePixelConversionMultiAndTrue, MultiAndTrueDistanceSelector, MultiAndTrueDistance>
                    (output, shape, transformation);
            else
                GenerateDistanceField<SimpleContourCombiner<MultiAndTrueDistanceSelector, MultiAndTrueDistance>, DistancePixelConversionMultiAndTrue, MultiAndTrueDistanceSelector, MultiAndTrueDistance>
                    (output, shape, transformation);
            // TODO : Error Correction method
        }


    }
}
