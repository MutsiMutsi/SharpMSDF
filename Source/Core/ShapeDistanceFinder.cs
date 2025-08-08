using System.Numerics;
using System.Runtime.CompilerServices;

namespace SharpMSDF.Core
{
	public unsafe struct ShapeMultiDistanceFinder
	{
		private OverlappingContourCombinerMultiDistance _contourCombiner;
		private readonly EdgeCache* _shapeEdgeCache;
		private readonly void* _memory; // For cleanup tracking

		public ShapeMultiDistanceFinder(ref Shape shape, Span<byte> workingMemory)
		{
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
		public MultiDistance Distance(ref Shape shape, Vector2 origin)
		{
			_contourCombiner.Reset(origin);

			EdgeCache* edgeCache = _shapeEdgeCache;

			for (int c = 0; c < shape.Contours.Count; c++)
			{
				var cr = shape.Contours[c];
				int count = cr.Count;
				if (count > 0)
				{
					var edgeSelector = _contourCombiner.EdgeSelector(c);

					// Setup initial prevEdge and curEdge safely regardless of count
					EdgeSegment prevEdge, curEdge;

					if (count == 1)
					{
						// Only one edge, prev and cur both point to it
						prevEdge = curEdge = shape.Edges[cr.Start];
					}
					else if (count == 2)
					{
						// Two edges, prev = first edge, cur = last edge
						prevEdge = shape.Edges[cr.Start];
						curEdge = shape.Edges[cr.Start + 1];
					}
					else
					{
						// More than 2 edges
						prevEdge = shape.Edges[cr.Start + count - 2];
						curEdge = shape.Edges[cr.Start + count - 1];
					}

					// Iterate all edges as nextEdge and call AddEdge
					for (int i = 0; i < count; i++)
					{
						EdgeSegment nextEdge = shape.Edges[cr.Start + i];
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