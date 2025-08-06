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
		public static unsafe void GenerateDistanceField(BitmapView output, Shape shape, SDFTransformation transformation)
		{
			// Create converter and distanceFinder (same)
			DistancePixelConversionMulti converter = new() { Mapping = transformation.DistanceMapping };
			ShapeMultiDistanceFinder distanceFinder = new(shape);

			// Assume BitmapView exposes a method/property to get the Span<float>
			Span<float> pixels = output.GetPixelSpan();

			fixed (float* pixelsPtr = pixels)
			{
				float* pixelsRaw = pixelsPtr;

				bool rightToLeft = false;
				for (int y = 0; y < output.Height; y++)
				{
					int row = shape.InverseYAxis ? output.Height - y - 1 : y;
					for (int col = 0; col < output.Width; col++)
					{
						int x = rightToLeft ? output.Width - col - 1 : col;
						Vector2 p = transformation.Projection.Unproject(new Vector2(x + .5f, y + .5f));
						MultiDistance dist = distanceFinder.Distance(p);
						// output.GetIndex adjusted for BitmapView’s internal logic
						float* pixel = pixelsRaw + output.GetIndex(x, row);
						converter.Convert(pixel, dist, x, row);
					} 
					rightToLeft = !rightToLeft;
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
		public static void GenerateMSDF(BitmapView output, Shape shape, SDFTransformation transformation, MSDFGeneratorConfig config = default)
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
