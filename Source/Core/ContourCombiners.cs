namespace SharpMSDF.Core
{
    internal static class DistanceUtils
    {
        public static void InitDistance(ref MultiDistanceSelector d)
        {
            InitDistance(ref d);
        }

		public static void InitDistance(ref double d) => d = -double.MaxValue;

		public static void InitDistance(ref MultiDistance d)
        {
            d.R = -double.MaxValue;
            d.G = -double.MaxValue;
            d.B = -double.MaxValue;
        }

        public static double ResolveDistance(ref MultiDistance d) =>
            Arithmetic.Median(d.R, d.G, d.B); // Implement median as needed
	}

   
    public class OverlappingContourCombinerMultiDistance
    {
        private Vector2 p;
        private readonly List<int> windings = new();
        private readonly List<MultiDistanceSelector> edgeSelectors = new();

        public OverlappingContourCombinerMultiDistance() {}

        public OverlappingContourCombinerMultiDistance(Shape shape) 
        {
            NonCtorInit(shape);
        }

        public void NonCtorInit(Shape shape)
        {
            foreach (var contour in shape.Contours)
            {
                windings.Add(contour.Winding());
                edgeSelectors.Add(new MultiDistanceSelector());
            }
        }

        public void Reset(Vector2 p)
        {
            this.p = p;
            foreach (var selector in edgeSelectors)
                selector.Reset(p);
        }

        public MultiDistanceSelector EdgeSelector(int i) => edgeSelectors[i];


        public MultiDistance Distance()
        {
            int contourCount = edgeSelectors.Count;

            var shapeEdgeSelector = new MultiDistanceSelector();
            var innerEdgeSelector = new MultiDistanceSelector();
            var outerEdgeSelector = new MultiDistanceSelector();

            shapeEdgeSelector.Reset(p);
            innerEdgeSelector.Reset(p);
            outerEdgeSelector.Reset(p);

            for (int i = 0; i < contourCount; ++i)
            {
				MultiDistance edgeDistance = edgeSelectors[i].Distance();
                shapeEdgeSelector.Merge(edgeSelectors[i]);

                double dist = DistanceUtils.ResolveDistance(ref edgeDistance);
                if (windings[i] > 0 && dist >= 0)
                    innerEdgeSelector.Merge(edgeSelectors[i]);
                if (windings[i] < 0 && dist <= 0)
                    outerEdgeSelector.Merge(edgeSelectors[i]);
            }

            MultiDistance shapeDistance = shapeEdgeSelector.Distance();
            MultiDistance innerDistance = innerEdgeSelector.Distance();
            MultiDistance outerDistance = outerEdgeSelector.Distance();

            double innerDist = DistanceUtils.ResolveDistance(ref innerDistance);
            double outerDist = DistanceUtils.ResolveDistance(ref outerDistance);

            MultiDistance distance = default!;
            DistanceUtils.InitDistance(ref distance);

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

    }

	public class OverlappingContourCombinerPerpendicularDistance
	{
		private Vector2 p;
		private readonly List<int> windings = new();
		private readonly List<PerpendicularDistanceSelector> edgeSelectors = new();

		public OverlappingContourCombinerPerpendicularDistance() { }

		public OverlappingContourCombinerPerpendicularDistance(Shape shape)
		{
			NonCtorInit(shape);
		}

		public void NonCtorInit(Shape shape)
		{
			foreach (var contour in shape.Contours)
			{
				windings.Add(contour.Winding());
				edgeSelectors.Add(new PerpendicularDistanceSelector());
			}
		}

		public void Reset(Vector2 p)
		{
			this.p = p;
			foreach (var selector in edgeSelectors)
				selector.Reset(p);
		}

		public PerpendicularDistanceSelector EdgeSelector(int i) => edgeSelectors[i];


		public double Distance()
		{
			int contourCount = edgeSelectors.Count;

			var shapeEdgeSelector = new PerpendicularDistanceSelector();
			var innerEdgeSelector = new PerpendicularDistanceSelector();
			var outerEdgeSelector = new PerpendicularDistanceSelector();

			shapeEdgeSelector.Reset(p);
			innerEdgeSelector.Reset(p);
			outerEdgeSelector.Reset(p);

			for (int i = 0; i < contourCount; ++i)
			{
				double dist = edgeSelectors[i].Distance();
				shapeEdgeSelector.Merge(edgeSelectors[i]);
				
                if (windings[i] > 0 && dist >= 0)
					innerEdgeSelector.Merge(edgeSelectors[i]);
				if (windings[i] < 0 && dist <= 0)
					outerEdgeSelector.Merge(edgeSelectors[i]);
			}

			double shapeDistance = shapeEdgeSelector.Distance();
			double innerDistance = innerEdgeSelector.Distance();
			double outerDistance = outerEdgeSelector.Distance();

			double innerDist = innerDistance;
            double outerDist = outerDistance;

			double distance = default!;
			DistanceUtils.InitDistance(ref distance);

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
						double contourRes = contourDist;
						if (Math.Abs(contourRes) < Math.Abs(outerDist) && contourRes > distance)
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
						double contourRes = contourDist;
						if (Math.Abs(contourRes) < Math.Abs(innerDist) && contourRes < distance)
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
					double res = contourDist;
					double distRes = distance;
					if (res * distRes >= 0 && Math.Abs(res) < Math.Abs(distRes))
						distance = contourDist;
				}
			}

			if (distance == shapeDistance)
				distance = shapeDistance;

			return distance;
		}

	}

}
