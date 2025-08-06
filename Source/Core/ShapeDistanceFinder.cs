using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharpMSDF.Core
{
    public class ShapePerpendicularDistanceFinder
    {
        public delegate double DistanceType(); // Will be overridden by TContourCombiner.DistanceType

        private readonly Shape Shape;
        private readonly OverlappingContourCombinerPerpendicularDistance ContourCombiner;
        private readonly EdgeCache[] ShapeEdgeCache; // real type: TContourCombiner.EdgeSelectorType.EdgeCache

        public ShapePerpendicularDistanceFinder(Shape shape)
        {
            this.Shape = shape;
            ContourCombiner = new OverlappingContourCombinerPerpendicularDistance();
            ContourCombiner.NonCtorInit(shape);
            ShapeEdgeCache = new EdgeCache[shape.EdgeCount()];
        }

        public unsafe double Distance(Vector2 origin)
        {
            ContourCombiner.Reset(origin);

            fixed (EdgeCache* edgeCacheStart = ShapeEdgeCache)
            {
                EdgeCache* edgeCache = edgeCacheStart;
                //int edgeCacheIndex = 0;

                for (int c = 0; c < Shape.Contours.Count; c++)
                {
                    var contour = Shape.Contours[c];
                    if (contour.Edges.Count > 0)
                    {
                        var edgeSelector = ContourCombiner.EdgeSelector(c);

                        EdgeSegment prevEdge = contour.Edges.Count >= 2
                            ? contour.Edges[contour.Edges.Count - 2]
                            : contour.Edges[0];

                        EdgeSegment curEdge = contour.Edges[^1];

                        for (int i = 0; i < contour.Edges.Count; i++)
                        {
                            EdgeSegment nextEdge = contour.Edges[i];
                            edgeSelector.AddEdge(edgeCache++, prevEdge, curEdge, nextEdge);
                            //ShapeEdgeCache[edgeCacheIndex++] = temp;
                            prevEdge = curEdge;
                            curEdge = nextEdge;
                        }
                    }
                }

            }
            return ContourCombiner.Distance();
        }

        public unsafe static double OneShotDistance(Shape shape, Vector2 origin)
        {
            var combiner = new OverlappingContourCombinerPerpendicularDistance();
            combiner.NonCtorInit(shape);
            combiner.Reset(origin);

            for (int i = 0; i < shape.Contours.Count; ++i)
            {
                var contour = shape.Contours[i];
                if (contour.Edges.Count == 0)
                    continue;

                var edgeSelector = combiner.EdgeSelector(i);

                EdgeSegment prevEdge = contour.Edges.Count >= 2
                    ? contour.Edges[contour.Edges.Count - 2]
                    : contour.Edges[0];

                EdgeSegment curEdge = contour.Edges[contour.Edges.Count - 1];

                foreach (var edgeSegment in contour.Edges)
                {
                    EdgeSegment nextEdge = edgeSegment;
                    var dummyCache = new EdgeCache(); // or default!
                    edgeSelector.AddEdge(&dummyCache, prevEdge, curEdge, nextEdge);

                    prevEdge = curEdge;
                    curEdge = nextEdge;
                }
            }

            return combiner.Distance();
        }
    }

	public class ShapeMultiDistanceFinder
	{
		public delegate double DistanceType(); // Will be overridden by TContourCombiner.DistanceType

		private readonly Shape Shape;
		private readonly OverlappingContourCombinerMultiDistance ContourCombiner;
		private readonly EdgeCache[] ShapeEdgeCache; // real type: TContourCombiner.EdgeSelectorType.EdgeCache

		public ShapeMultiDistanceFinder(Shape shape)
		{
			this.Shape = shape;
			ContourCombiner = new OverlappingContourCombinerMultiDistance();
			ContourCombiner.NonCtorInit(shape);
			ShapeEdgeCache = new EdgeCache[shape.EdgeCount()];
		}

		public unsafe MultiDistance Distance(Vector2 origin)
		{
			ContourCombiner.Reset(origin);

			fixed (EdgeCache* edgeCacheStart = ShapeEdgeCache)
			{
				EdgeCache* edgeCache = edgeCacheStart;
				//int edgeCacheIndex = 0;

				for (int c = 0; c < Shape.Contours.Count; c++)
				{
					var contour = Shape.Contours[c];
					if (contour.Edges.Count > 0)
					{
						var edgeSelector = ContourCombiner.EdgeSelector(c);

						EdgeSegment prevEdge = contour.Edges.Count >= 2
							? contour.Edges[contour.Edges.Count - 2]
							: contour.Edges[0];

						EdgeSegment curEdge = contour.Edges[^1];

						for (int i = 0; i < contour.Edges.Count; i++)
						{
							EdgeSegment nextEdge = contour.Edges[i];
							edgeSelector.AddEdge(edgeCache++, prevEdge, curEdge, nextEdge);
							//ShapeEdgeCache[edgeCacheIndex++] = temp;
							prevEdge = curEdge;
							curEdge = nextEdge;
						}
					}
				}

			}
			return ContourCombiner.Distance();
		}

		public unsafe static MultiDistance OneShotDistance(Shape shape, Vector2 origin)
		{
			var combiner = new OverlappingContourCombinerMultiDistance();
			combiner.NonCtorInit(shape);
			combiner.Reset(origin);

			for (int i = 0; i < shape.Contours.Count; ++i)
			{
				var contour = shape.Contours[i];
				if (contour.Edges.Count == 0)
					continue;

				var edgeSelector = combiner.EdgeSelector(i);

				EdgeSegment prevEdge = contour.Edges.Count >= 2
					? contour.Edges[contour.Edges.Count - 2]
					: contour.Edges[0];

				EdgeSegment curEdge = contour.Edges[contour.Edges.Count - 1];

				foreach (var edgeSegment in contour.Edges)
				{
					EdgeSegment nextEdge = edgeSegment;
					var dummyCache = new EdgeCache(); // or default!
					edgeSelector.AddEdge(&dummyCache, prevEdge, curEdge, nextEdge);

					prevEdge = curEdge;
					curEdge = nextEdge;
				}
			}

			return combiner.Distance();
		}
	}
}
