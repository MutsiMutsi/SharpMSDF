using System.Runtime.CompilerServices;

namespace SharpMSDF.Core
{
	public unsafe struct ShapeMultiDistanceFinder
	{
		private readonly Shape _shape;
		private OverlappingContourCombinerMultiDistance _contourCombiner;
		private readonly EdgeCache* _shapeEdgeCache;
		private readonly void* _memory; // For cleanup tracking

		public ShapeMultiDistanceFinder(Shape shape, Span<byte> workingMemory)
		{
			_shape = shape;

			int edgeCacheSize = shape.EdgeCount() * sizeof(EdgeCache);
			int combinerMemorySize = OverlappingContourCombinerMultiDistance.GetRequiredMemorySize(shape.Contours.Count);
			int totalRequiredSize = edgeCacheSize + combinerMemorySize;

			if (workingMemory.Length < totalRequiredSize)
				throw new ArgumentException($"Working memory too small. Required: {totalRequiredSize}, provided: {workingMemory.Length}");

			fixed (byte* memPtr = workingMemory)
			{
				_memory = memPtr;
				_shapeEdgeCache = (EdgeCache*)memPtr;

				var combinerMemory = workingMemory.Slice(edgeCacheSize, combinerMemorySize);
				_contourCombiner = new OverlappingContourCombinerMultiDistance(shape, combinerMemory);
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public MultiDistance Distance(Vector2 origin)
		{
			_contourCombiner.Reset(origin);

			EdgeCache* edgeCache = _shapeEdgeCache;

			for (int c = 0; c < _shape.Contours.Count; c++)
			{
				var contour = _shape.Contours[c];
				if (contour.Edges.Count > 0)
				{
					var edgeSelector = _contourCombiner.EdgeSelector(c);

					EdgeSegment prevEdge = contour.Edges.Count >= 2
						? contour.Edges[contour.Edges.Count - 2]
						: contour.Edges[0];

					EdgeSegment curEdge = contour.Edges[^1];

					for (int i = 0; i < contour.Edges.Count; i++)
					{
						EdgeSegment nextEdge = contour.Edges[i];
						edgeSelector->AddEdge(edgeCache++, prevEdge, curEdge, nextEdge);
						prevEdge = curEdge;
						curEdge = nextEdge;
					}
				}
			}

			return _contourCombiner.Distance();
		}

		public static int GetRequiredMemorySize(Shape shape)
		{
			int edgeCacheSize = shape.EdgeCount() * sizeof(EdgeCache);
			int combinerMemorySize = OverlappingContourCombinerMultiDistance.GetRequiredMemorySize(shape.Contours.Count);
			return edgeCacheSize + combinerMemorySize;
		}
	}
}