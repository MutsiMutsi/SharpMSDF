﻿
namespace SharpMSDF.Core
{
	/*public interface IDistanceSelector<TDistance>
    {
        /// <summary>
        /// Reset any internal state for a new query point p.
        /// </summary>
        unsafe void AddEdge(EdgeCache* cache, EdgeSegment prevEdge, EdgeSegment edge, EdgeSegment nextEdge);

        /// <summary>
        /// Reset any internal state for a new query point p.
        /// </summary>
        void Reset(Vector2 p);

        /// <summary>
        /// Merge with another selector (for parallel reduction).
        /// </summary>
        void Merge(IDistanceSelector<TDistance> other);

        /// <summary>
        /// Return the final distance value of type TDistance.
        /// </summary>
        TDistance Distance();
    }
    */
	public struct MultiDistance
	{
		public double R, G, B;
	}

	/*public struct MultiAndTrueDistance
    {
        public double R, G, B, A;
    }*/

	public struct EdgeCache
	{
		public Vector2 Point { get; set; }
		public double AbsDistance { get; set; }
		public double ADomainDistance, BDomainDistance;
		public double APerpDistance, BPerpDistance;
		public EdgeCache()
		{
			Point = default;
			AbsDistance = 0;
			ADomainDistance = BDomainDistance = 0;
			APerpDistance = BPerpDistance = 0;
		}
	}

	/*public class TrueDistanceSelector : IDistanceSelector<double>
    {
        private Vector2 _p;
        private SignedDistance _minDistance = new SignedDistance();

        private const double DISTANCE_DELTA_FACTOR = 1.001;

        public void Reset(Vector2 p)
        {
            double delta = DISTANCE_DELTA_FACTOR * (p - _p).Length();
            _minDistance.Distance += Arithmetic.NonZeroSign(_minDistance.Distance) * delta;
            _p = p;
        }

        public unsafe void AddEdge(EdgeCache* cache, EdgeSegment prevEdge, EdgeSegment edge, EdgeSegment nextEdge)
        {
            double delta = DISTANCE_DELTA_FACTOR * (_p - (*cache).Point).Length();
            if ((*cache).AbsDistance - delta <= Math.Abs(_minDistance.Distance))
            {
                var distance = edge.SignedDistance(_p, out _);
                if (distance < _minDistance)
                    _minDistance = distance;
                (*cache).Point = _p;
                (*cache).AbsDistance = Math.Abs(distance.Distance);
            }
        }

        public void Merge(IDistanceSelector<double> other)
        {
            if (other is TrueDistanceSelector otherT)
            {
                if (otherT._minDistance < _minDistance)
                    _minDistance = otherT._minDistance;
            }
            else throw new("Wrong type to merge");
        }

        

        public double Distance() => _minDistance.Distance;

    }

    */

	public class PerpendicularDistanceSelector : PerpendicularDistanceSelectorBase
	{
		private Vector2 _p;
		public double DistanceType;  // in C# you can drop this: use double directly

		public void Reset(Vector2 p)
		{
			double delta = DISTANCE_DELTA_FACTOR * (p - _p).Length();
			base.Reset(delta);
			_p = p;
		}

		public void Merge(PerpendicularDistanceSelector other)
		{
			Merge(other);
		}

		public unsafe void AddEdge(EdgeCache* cache, EdgeSegment prevEdge, EdgeSegment edge, EdgeSegment nextEdge)
		{
			if (IsEdgeRelevant((*cache), edge, _p))
			{
				double param;
				var dist = edge.SignedDistance(_p, out param);
				AddEdgeTrueDistance(edge, dist, param);
				(*cache).Point = _p;
				(*cache).AbsDistance = Math.Abs(dist.Distance);

				Vector2 ap = _p - edge.Point(0);
				Vector2 bp = _p - edge.Point(1);
				Vector2 aDir = edge.Direction(0).Normalize(true);
				Vector2 bDir = edge.Direction(1).Normalize(true);
				Vector2 prevDir = prevEdge.Direction(1).Normalize(true);
				Vector2 nextDir = nextEdge.Direction(0).Normalize(true);

				double add = Vector2.Dot(ap, (prevDir + aDir).Normalize());
				double bdd = -Vector2.Dot(bp, (bDir + nextDir).Normalize());

				if (add > 0)
				{
					double pd = dist.Distance;
					if (GetPerpendicularDistance(ref pd, ap, -aDir))
						AddEdgePerpendicularDistance(pd = -pd);
					(*cache).APerpDistance = pd;
				}
				if (bdd > 0)
				{
					double pd = dist.Distance;
					if (GetPerpendicularDistance(ref pd, bp, bDir))
						AddEdgePerpendicularDistance(pd);
					(*cache).BPerpDistance = pd;
				}
				(*cache).ADomainDistance = add;
				(*cache).BDomainDistance = bdd;
			}
		}

		public double Distance() => ComputeDistance(_p);
	}

	public class PerpendicularDistanceSelectorBase
	{

		internal SignedDistance _minTrueDistance = new SignedDistance();
		internal double _minNegPerp, _minPosPerp;
		internal EdgeSegment _nearEdge;
		internal double _nearEdgeParam;

		public const double DISTANCE_DELTA_FACTOR = 1.001;

		public PerpendicularDistanceSelectorBase()
		{
			_minNegPerp = -Math.Abs(_minTrueDistance.Distance);
			_minPosPerp = Math.Abs(_minTrueDistance.Distance);
			_nearEdge = null;
			_nearEdgeParam = 0;
		}

		public static bool GetPerpendicularDistance(ref double distance, Vector2 ep, Vector2 edgeDir)
		{
			double ts = Vector2.Dot(ep, edgeDir);
			if (ts > 0)
			{
				double perp = Vector2.Cross(ep, edgeDir);
				if (Math.Abs(perp) < Math.Abs(distance))
				{
					distance = perp;
					return true;
				}
			}
			return false;
		}

		public void Reset(double delta)
		{
			_minTrueDistance.Distance += Arithmetic.NonZeroSign(_minTrueDistance.Distance) * delta;
			_minNegPerp = -Math.Abs(_minTrueDistance.Distance);
			_minPosPerp = Math.Abs(_minTrueDistance.Distance);
			_nearEdge = null;
			_nearEdgeParam = 0;
		}

		public bool IsEdgeRelevant(EdgeCache cache, EdgeSegment edge, Vector2 p)
		{
			double delta = DISTANCE_DELTA_FACTOR * (p - cache.Point).Length();
			return
				cache.AbsDistance - delta <= Math.Abs(_minTrueDistance.Distance)
				|| Math.Abs(cache.ADomainDistance) < delta
				|| Math.Abs(cache.BDomainDistance) < delta
				|| (cache.ADomainDistance > 0 && (
						cache.APerpDistance < 0
							? cache.APerpDistance + delta >= _minNegPerp
							: cache.APerpDistance - delta <= _minPosPerp
				   ))
				|| (cache.BDomainDistance > 0 && (
						cache.BPerpDistance < 0
							? cache.BPerpDistance + delta >= _minNegPerp
							: cache.BPerpDistance - delta <= _minPosPerp
				   ));
		}

		internal void AddEdgeTrueDistance(EdgeSegment edge, SignedDistance dist, double param)
		{
			if (dist < _minTrueDistance)
			{
				_minTrueDistance = dist;
				_nearEdge = edge;
				_nearEdgeParam = param;
			}
		}

		internal void AddEdgePerpendicularDistance(double d)
		{
			if (d <= 0 && d > _minNegPerp) _minNegPerp = d;
			if (d >= 0 && d < _minPosPerp) _minPosPerp = d;
		}

		public void Merge(PerpendicularDistanceSelectorBase other)
		{
			if (other._minTrueDistance < _minTrueDistance)
			{
				_minTrueDistance = other._minTrueDistance;
				_nearEdge = other._nearEdge;
				_nearEdgeParam = other._nearEdgeParam;
			}
			if (other._minNegPerp > _minNegPerp) _minNegPerp = other._minNegPerp;
			if (other._minPosPerp < _minPosPerp) _minPosPerp = other._minPosPerp;
		}

		internal double ComputeDistance(Vector2 p)
		{
			double best = _minTrueDistance.Distance < 0 ? _minNegPerp : _minPosPerp;
			if (_nearEdge != null)
			{
				var sd = _minTrueDistance;
				_nearEdge.DistanceToPerpendicularDistance(ref sd, p, _nearEdgeParam);
				if (Math.Abs(sd.Distance) < Math.Abs(best))
					best = sd.Distance;
			}
			return best;
		}

		public SignedDistance TrueDistance() => _minTrueDistance;

		// --- helper: Vector2.Cross(Vector2,Vector2), Arithmetic.NonZeroSign(double) must be provided elsewhere ---
	}

	public class MultiDistanceSelector
	{
		protected Vector2 _p;
		protected PerpendicularDistanceSelectorBase _r = new PerpendicularDistanceSelectorBase();
		protected PerpendicularDistanceSelectorBase _g = new PerpendicularDistanceSelectorBase();
		protected PerpendicularDistanceSelectorBase _b = new PerpendicularDistanceSelectorBase();

		public void Reset(Vector2 p)
		{
			double delta = PerpendicularDistanceSelectorBase.DISTANCE_DELTA_FACTOR * (p - _p).Length();
			_r.Reset(delta);
			_g.Reset(delta);
			_b.Reset(delta);
			_p = p;
		}

		public unsafe void AddEdge(EdgeCache* cache, EdgeSegment prev, EdgeSegment edge, EdgeSegment next)
		{
			EdgeColor color = edge.Color;
			bool doR = (color & EdgeColor.Red) != 0 && _r.IsEdgeRelevant((*cache), edge, _p);
			bool doG = (color & EdgeColor.Green) != 0 && _g.IsEdgeRelevant((*cache), edge, _p);
			bool doB = (color & EdgeColor.Blue) != 0 && _b.IsEdgeRelevant((*cache), edge, _p);
			if (doR || doG || doB)
			{
				double param;
				var dist = edge.SignedDistance(_p, out param);
				if (doR) _r.AddEdgeTrueDistance(edge, dist, param);
				if (doG) _g.AddEdgeTrueDistance(edge, dist, param);
				if (doB) _b.AddEdgeTrueDistance(edge, dist, param);
				(*cache).Point = _p;
				(*cache).AbsDistance = Math.Abs(dist.Distance);

				Vector2 ap = _p - edge.Point(0);
				Vector2 bp = _p - edge.Point(1);
				Vector2 aDir = edge.Direction(0).Normalize(true);
				Vector2 bDir = edge.Direction(1).Normalize(true);
				Vector2 prevDir = prev.Direction(1).Normalize(true);
				Vector2 nextDir = next.Direction(0).Normalize(true);
				double add = Vector2.Dot(ap, (prevDir + aDir).Normalize(true));
				double bdd = -Vector2.Dot(bp, (bDir + nextDir).Normalize(true));

				if (add > 0)
				{
					double pd = dist.Distance;
					if (PerpendicularDistanceSelectorBase.GetPerpendicularDistance(ref pd, ap, -aDir))
					{
						pd = -pd;
						if (doR) _r.AddEdgePerpendicularDistance(pd);
						if (doG) _g.AddEdgePerpendicularDistance(pd);
						if (doB) _b.AddEdgePerpendicularDistance(pd);
					}
					(*cache).APerpDistance = pd;
				}
				if (bdd > 0)
				{
					double pd = dist.Distance;
					if (PerpendicularDistanceSelectorBase.GetPerpendicularDistance(ref pd, bp, bDir))
					{
						if (doR) _r.AddEdgePerpendicularDistance(pd);
						if (doG) _g.AddEdgePerpendicularDistance(pd);
						if (doB) _b.AddEdgePerpendicularDistance(pd);
					}
					(*cache).BPerpDistance = pd;
				}
				(*cache).ADomainDistance = add;
				(*cache).BDomainDistance = bdd;
			}
		}

		public void Merge(MultiDistanceSelector other)
		{
			_r.Merge(other._r);
			_g.Merge(other._g);
			_b.Merge(other._b);
		}

		public MultiDistance Distance()
		{
			return new MultiDistance
			{
				R = _r.ComputeDistance(_p),
				G = _g.ComputeDistance(_p),
				B = _b.ComputeDistance(_p)
			};
		}

		public SignedDistance TrueDistance()
		{
			var d = _r.TrueDistance();
			if (_g.TrueDistance() < d) d = _g.TrueDistance();
			if (_b.TrueDistance() < d) d = _b.TrueDistance();
			return d;
		}
	}

	/*public class MultiAndTrueDistanceSelector : MultiDistanceSelector, IDistanceSelector<MultiAndTrueDistance>
    {
        new public MultiAndTrueDistance Distance()
        {
            var md = base.Distance();
            var td = base.TrueDistance();
            return new MultiAndTrueDistance { R = md.R, G = md.G, B = md.B, A = td.Distance };
        }

        public void Merge(IDistanceSelector<MultiAndTrueDistance> other)
        {
            if (other is MultiAndTrueDistanceSelector otherM)
            {
                _r.Merge(otherM._r);
                _g.Merge(otherM._g);
                _b.Merge(otherM._b);

            }
            else throw new("Wrong type to merge");
        }

    }*/
}