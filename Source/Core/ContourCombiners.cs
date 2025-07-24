namespace SharpMSDF.Core
{
    // todo: Fill

    /// <summary>
    /// Simply selects the nearest contour.
    /// </summary>
    public class SimpleContourCombiner<TSelector> where TSelector : new()
    {
        public delegate dynamic DistanceType(); // You can change this to double, MultiDistance, etc.

        public TSelector shapeEdgeSelector;

        public SimpleContourCombiner(Shape shape)
        {
            shapeEdgeSelector = new TSelector();
            // initialize from shape if needed
        }

        public void Reset(Vector2 p)
        {
            // Implement
        }

        public TSelector EdgeSelector(int i)
        {
            return shapeEdgeSelector;
        }

        public dynamic Distance()
        {
            return default;
        }
    }

    /// <summary>
    /// Selects the nearest contour that actually forms a border between filled and unfilled area.
    /// </summary>
    public class OverlappingContourCombiner<TSelector> where TSelector : new()
    {
        public delegate dynamic DistanceType(); // Adjust as needed

        private Vector2 p;
        private List<int> windings = new List<int>();
        private List<TSelector> edgeSelectors = new List<TSelector>();

        public OverlappingContourCombiner(Shape shape)
        {
            // initialize edgeSelectors from shape.contours.Count, if needed
        }

        public void Reset(Vector2 point)
        {
            p = point;
            // Clear/initialize windings and edgeSelectors if needed
        }

        public TSelector EdgeSelector(int i)
        {
            return edgeSelectors[i];
        }

        public dynamic Distance()
        {
            return default;
        }
    }
}
