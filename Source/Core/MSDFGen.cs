using System.Runtime.CompilerServices;

namespace SharpMSDF.Core
{
	public static class MSDFGen
	{
		public abstract unsafe class DistancePixelConversion<TDistance>
		{
			public required DistanceMapping Mapping;

			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			public abstract void Convert(float* pixels, TDistance distance, int s = 0, int s2 = 0);
		}

		public unsafe class DistancePixelConversionSingle : DistancePixelConversion<double>
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			public override void Convert(float* pixels, double distance, int s = 0, int s2 = 0)
			{
				*pixels = (float)Mapping[distance];
			}
		}

		public unsafe class DistancePixelConversionMulti : DistancePixelConversion<MultiDistance>
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			public override void Convert(float* pixels, MultiDistance distance, int s = 0, int s2 = 0)
			{
				*pixels = (float)Mapping[distance.R];
				*(pixels + 1) = (float)Mapping[distance.G];
				*(pixels + 2) = (float)Mapping[distance.B];
			}
		}

		/* public unsafe class DistancePixelConversionMultiAndTrue : DistancePixelConversion<MultiAndTrueDistance>
		 {
			 [MethodImpl(MethodImplOptions.AggressiveInlining)]
			 public override void Convert(float* pixels, MultiAndTrueDistance distance, int s=0, int s2=0)
			 {
				 *pixels = (float)Mapping[distance.R];
				 *(pixels+1) = (float)Mapping[distance.G];
				 *(pixels+2) = (float)Mapping[distance.B];
				 *(pixels+3) = (float)Mapping[distance.A];
			 }
		 }*/

		/// <summary>
		/// Generates a conventional single-channel signed distance field.
		/// </summary>
		public static unsafe void GenerateDistanceField(BitmapRef output, Shape shape, SDFTransformation transformation)
		{
			// 1. Create the converter 
			// TODO: potential of less H-Allocation
			DistancePixelConversionMulti converter = new() { Mapping = transformation.DistanceMapping };
			// 2. Create your combiner‐driven distance finder
			ShapeMultiDistanceFinder distanceFinder = new(shape);

			// 3. Parallel loop over rows
			bool rightToLeft = false;

			fixed (float* arrayFixed = output.Pixels)
			{
				// used to trick compiler into thinking this is not fixed when dealing with lambda expression
				float* pixels = arrayFixed;

				for (int y = 0; y < output.SubHeight; y++)
				//Parallel.For(0, output.SubHeight, y =>
				{
					int row = shape.InverseYAxis ? output.SubHeight - y - 1 : y;
					for (int col = 0; col < output.SubWidth; col++)
					{
						int x = rightToLeft ? output.SubWidth - col - 1 : col;
						// unproject into Shape‐space
						Vector2 p = transformation.Projection.Unproject(new Vector2(x + .5f, y + .5f));
						// get the signed‐distance
						MultiDistance dist = distanceFinder.Distance(p);
						// write into the pixel Buffer
						float* pixel = pixels + output.GetIndex(x, row);
						converter.Convert(pixel, dist, x, row);
					}
					rightToLeft = !rightToLeft; // flip for “staggered” ordering
				}
			}
		}
		/*
		/// <summary>
		/// Generates a conventional single-channel signed distance field.
		/// </summary>
		public static void GenerateSDF(BitmapRefSingle output, Shape shape, SDFTransformation transformation, GeneratorConfig config = default)
		{
			if (config.OverlapSupport)
				GenerateDistanceField<OverlappingContourCombiner<TrueDistanceSelector, double>, DistancePixelConversionSingle, TrueDistanceSelector, double>
					(output, shape, transformation);
			else
				GenerateDistanceField<SimpleContourCombiner<TrueDistanceSelector, double>, DistancePixelConversionSingle, TrueDistanceSelector, double>
					(output, shape, transformation);
		}

		/// <summary>
		/// Generates a single-channel signed perpendicular distance field.
		/// </summary>
		public static void GeneratePSDF(BitmapRefSingle output, Shape shape, SDFTransformation transformation, GeneratorConfig config = default)
		{
			if (config.OverlapSupport)
				GenerateDistanceField<OverlappingContourCombiner<PerpendicularDistanceSelector, double>, DistancePixelConversionSingle, PerpendicularDistanceSelector, double>
					(output, shape, transformation);
			else
				GenerateDistanceField<SimpleContourCombiner<PerpendicularDistanceSelector, double>, DistancePixelConversionSingle, PerpendicularDistanceSelector, double>
					(output, shape, transformation);
		}
		*/
		/// <summary>
		/// Generates a multi-channel signed distance field. Edge colors must be assigned first! (See edgeColoringSimple)
		/// </summary>
		public static void GenerateMSDF(BitmapRef output, Shape shape, SDFTransformation transformation, MSDFGeneratorConfig config = default)
		{
			GenerateDistanceField(output, shape, transformation);
			//MSDFErrorCorrection.ErrorCorrection(output, shape, transformation, config);
		}

		/// <summary>
		/// Generates a multi-channel signed distance field with true distance in the alpha channel. Edge colors must be assigned first.
		/// </summary>
		/*public static void GenerateMTSDF(BitmapRefMultiAndTrue output, Shape shape, SDFTransformation transformation, MSDFGeneratorConfig config = default)
		{
			if (config.OverlapSupport)
				GenerateDistanceField<OverlappingContourCombiner<MultiAndTrueDistanceSelector, MultiAndTrueDistance>, DistancePixelConversionMultiAndTrue, MultiAndTrueDistanceSelector, MultiAndTrueDistance>
					(output, shape, transformation);
			else
				GenerateDistanceField<SimpleContourCombiner<MultiAndTrueDistanceSelector, MultiAndTrueDistance>, DistancePixelConversionMultiAndTrue, MultiAndTrueDistanceSelector, MultiAndTrueDistance>
					(output, shape, transformation);
			MSDFErrorCorrection.ErrorCorrection(output, shape, transformation, config);
		}
		*/

	}
}
