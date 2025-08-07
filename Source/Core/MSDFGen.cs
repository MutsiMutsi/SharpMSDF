using System;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace SharpMSDF.Core
{
	public static class MSDFGen
	{
		public unsafe struct DistancePixelConversionMulti
		{
			public required DistanceMapping Mapping { get; init; }

			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			public readonly void Convert(float* pixels, MultiDistance distance, int s = 0, int s2 = 0)
			{
				*pixels = (float)Mapping[distance.R];
				*(pixels + 1) = (float)Mapping[distance.G];
				*(pixels + 2) = (float)Mapping[distance.B];
			}
		}

		/// <summary>
		/// Generates a conventional single-channel signed distance field.
		/// </summary>
		public static unsafe void GenerateDistanceField(BitmapView output, Shape shape, SDFTransformation transformation, Span<byte> workingMemory)
		{
			var converter = new DistancePixelConversionMulti { Mapping = transformation.DistanceMapping };
			var distanceFinder = new ShapeMultiDistanceFinder(ref shape, workingMemory);

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
						MultiDistance dist = distanceFinder.Distance(ref shape, p);
						float* pixel = pixelsRaw + output.GetIndex(x, row);
						converter.Convert(pixel, dist, x, row);
					}
					rightToLeft = !rightToLeft;
				}
			}
		}

		/// <summary>
		/// Generates a multi-channel signed distance field. Edge colors must be assigned first! (See edgeColoringSimple)
		/// </summary>
		public static void GenerateMSDF(BitmapView output, Shape shape, SDFTransformation transformation, Span<byte> workingMemory, MSDFGeneratorConfig config = default)
		{
			GenerateDistanceField(output, shape, transformation, workingMemory);
			//MSDFErrorCorrection.ErrorCorrection(output, shape, transformation, config);
		}

		/// <summary>
		/// Calculates the required working memory size for the given shape.
		/// </summary>
		public static int GetRequiredWorkingMemorySize(Shape shape)
		{
			return ShapeMultiDistanceFinder.GetRequiredMemorySize(shape);
		}
	}
}
