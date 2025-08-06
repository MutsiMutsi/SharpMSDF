using System;
using System.Runtime.CompilerServices;

namespace SharpMSDF.Core
{
	internal static class DistanceUtils
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static double ResolveDistance(ref MultiDistance d) =>
			Arithmetic.Median(d.R, d.G, d.B);
	}

	public unsafe struct OverlappingContourCombinerMultiDistance
	{
		private Vector2 p;
		private readonly int* windings;
		private readonly MultiDistanceSelector* edgeSelectors;
		private readonly int contourCount;
		private readonly void* memory; // For cleanup tracking

		public OverlappingContourCombinerMultiDistance(Shape shape, Span<byte> workingMemory)
		{
			contourCount = shape.Contours.Count;

			// Calculate memory requirements
			int windingsSize = contourCount * sizeof(int);
			int selectorsSize = contourCount * sizeof(MultiDistanceSelector);
			int totalSize = windingsSize + selectorsSize;

			if (workingMemory.Length < totalSize)
				throw new ArgumentException($"Working memory too small. Required: {totalSize}, provided: {workingMemory.Length}");

			fixed (byte* memPtr = workingMemory)
			{
				memory = memPtr;
				windings = (int*)memPtr;
				edgeSelectors = (MultiDistanceSelector*)(memPtr + windingsSize);
			}

			p = default;

			// Initialize data
			for (int i = 0; i < contourCount; i++)
			{
				windings[i] = shape.Contours[i].Winding();
				edgeSelectors[i] = new MultiDistanceSelector();
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void Reset(Vector2 newP)
		{
			p = newP;
			for (int i = 0; i < contourCount; i++)
			{
				edgeSelectors[i].Reset(newP);
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public MultiDistanceSelector* EdgeSelector(int i) => &edgeSelectors[i];

		public MultiDistance Distance()
		{
			var shapeEdgeSelector = new MultiDistanceSelector();
			var innerEdgeSelector = new MultiDistanceSelector();
			var outerEdgeSelector = new MultiDistanceSelector();

			shapeEdgeSelector.Reset(p);
			innerEdgeSelector.Reset(p);
			outerEdgeSelector.Reset(p);

			for (int i = 0; i < contourCount; ++i)
			{
				MultiDistance edgeDistance = edgeSelectors[i].Distance();
				shapeEdgeSelector.Merge(ref edgeSelectors[i]);

				double dist = DistanceUtils.ResolveDistance(ref edgeDistance);
				if (windings[i] > 0 && dist >= 0)
					innerEdgeSelector.Merge(ref edgeSelectors[i]);
				if (windings[i] < 0 && dist <= 0)
					outerEdgeSelector.Merge(ref edgeSelectors[i]);
			}

			MultiDistance shapeDistance = shapeEdgeSelector.Distance();
			MultiDistance innerDistance = innerEdgeSelector.Distance();
			MultiDistance outerDistance = outerEdgeSelector.Distance();

			double innerDist = DistanceUtils.ResolveDistance(ref innerDistance);
			double outerDist = DistanceUtils.ResolveDistance(ref outerDistance);

			MultiDistance distance = new MultiDistance
			{
				R = -double.MaxValue,
				G = -double.MaxValue,
				B = -double.MaxValue
			};

			int winding = 0;

			if (innerDist >= 0 && Math.Abs(innerDist) <= Math.Abs(outerDist))
			{
				distance = innerDistance;
				winding = 1;
				for (int i = 0; i < contourCount; ++i)
				{
					if (windings[i] > 0)
					{
						var contourDist = edgeSelectors[i].Distance();
						double contourRes = DistanceUtils.ResolveDistance(ref contourDist);
						if (Math.Abs(contourRes) < Math.Abs(outerDist) && contourRes > DistanceUtils.ResolveDistance(ref distance))
							distance = contourDist;
					}
				}
			}
			else if (outerDist <= 0 && Math.Abs(outerDist) < Math.Abs(innerDist))
			{
				distance = outerDistance;
				winding = -1;
				for (int i = 0; i < contourCount; ++i)
				{
					if (windings[i] < 0)
					{
						var contourDist = edgeSelectors[i].Distance();
						double contourRes = DistanceUtils.ResolveDistance(ref contourDist);
						if (Math.Abs(contourRes) < Math.Abs(innerDist) && contourRes < DistanceUtils.ResolveDistance(ref distance))
							distance = contourDist;
					}
				}
			}
			else
			{
				return shapeDistance;
			}

			for (int i = 0; i < contourCount; ++i)
			{
				if (windings[i] != winding)
				{
					var contourDist = edgeSelectors[i].Distance();
					double res = DistanceUtils.ResolveDistance(ref contourDist);
					double distRes = DistanceUtils.ResolveDistance(ref distance);
					if (res * distRes >= 0 && Math.Abs(res) < Math.Abs(distRes))
						distance = contourDist;
				}
			}

			if (DistanceUtils.ResolveDistance(ref distance) == DistanceUtils.ResolveDistance(ref shapeDistance))
				distance = shapeDistance;

			return distance;
		}

		public static int GetRequiredMemorySize(int contourCount)
		{
			return (contourCount * sizeof(int)) + (contourCount * sizeof(MultiDistanceSelector));
		}
	}
}