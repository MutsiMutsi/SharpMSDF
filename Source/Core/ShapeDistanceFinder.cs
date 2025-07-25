using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharpMSDF.Core
{
    public class ShapeDistanceFinder<TCombiner, TDistanceSelector, TDistance>
        where  TDistanceSelector : IDistanceSelector<TDistance>
        where TCombiner : ContourCombiner<TDistanceSelector,TDistance>, new()
    {
        public delegate double DistanceType(); // Will be overridden by TContourCombiner.DistanceType

        private readonly Shape Shape;
        private readonly ContourCombiner<TDistanceSelector, TDistance> ContourCombiner;
        private readonly List<EdgeCache> ShapeEdgeCache; // real type: TContourCombiner.EdgeSelectorType.EdgeCache

        public ShapeDistanceFinder(Shape shape)
        {
            this.Shape = shape;
            ContourCombiner = new TCombiner();
            ContourCombiner.NonCtorInit(shape);
            ShapeEdgeCache = new List<EdgeCache>(); // will cast to correct type later
        }

        public TDistance Distance(Vector2 origin)
        {
            ContourCombiner.Reset(origin);

            //TODO : sus
            ShapeEdgeCache.Clear();
            //int edgeCacheIndex = 0;

            for (int c = 0; c < Shape.Contours.Count; c++)
            {
                var contour = Shape.Contours[c];
                if (contour.Edges.Count > 0)
                {
                    var edgeSelector = ContourCombiner.EdgeSelector(c);

                    EdgeSegment prevEdge = contour.Edges.Count >= 2
                        ? contour.Edges[contour.Edges.Count - 2].Segment
                        : contour.Edges[0].Segment;

                    EdgeSegment curEdge = contour.Edges[^1].Segment;

                    for (int i = 0; i < contour.Edges.Count; i++)
                    {
                        EdgeSegment nextEdge = contour.Edges[i].Segment;
                        var temp = new EdgeCache();
                        edgeSelector.AddEdge(ref temp, prevEdge, curEdge, nextEdge);
                        ShapeEdgeCache.Add(temp);
                        //ShapeEdgeCache[edgeCacheIndex++] = temp;
                        prevEdge = curEdge;
                        curEdge = nextEdge;
                    }
                }
            }

            return ContourCombiner.Distance();
        }

        public static object OneShotDistance(Shape shape, Vector2 origin)
        {
            var combiner = new TCombiner();
            combiner.NonCtorInit(shape);
            combiner.Reset(origin);

            for (int i = 0; i < shape.Contours.Count; ++i)
            {
                var contour = shape.Contours[i];
                if (contour.Edges.Count == 0)
                    continue;

                var edgeSelector = combiner.EdgeSelector(i);

                EdgeSegment prevEdge = contour.Edges.Count >= 2
                    ? contour.Edges[contour.Edges.Count - 2].Segment
                    : contour.Edges[0].Segment;

                EdgeSegment curEdge = contour.Edges[contour.Edges.Count - 1].Segment;

                foreach (var edgeHolder in contour.Edges)
                {
                    EdgeSegment nextEdge = edgeHolder.Segment;
                    var dummyCache = new EdgeCache(); // or default!
                    edgeSelector.AddEdge(ref dummyCache, prevEdge, curEdge, nextEdge);

                    prevEdge = curEdge;
                    curEdge = nextEdge;
                }
            }

            return combiner.Distance();
        }
    }
}
