using System;
using System.Runtime.CompilerServices;

namespace SharpMSDF.Core
{
	public struct MultiDistance
	{
		public double R, G, B;
	}

	public struct EdgeCache
	{
		public Vector2 Point;
		public double AbsDistance;
		public double ADomainDistance, BDomainDistance;
		public double APerpDistance, BPerpDistance;
	}

	public unsafe ref struct PerpendicularDistanceSelector
	{
		private Vector2 _p;
		private PerpendicularDistanceSelectorBase _base;

		public const double DISTANCE_DELTA_FACTOR = 1.001;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void Reset(Vector2 p)
		{
			double delta = DISTANCE_DELTA_FACTOR * (p - _p).Length();
			_base.Reset(delta);
			_p = p;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void Merge(ref PerpendicularDistanceSelector other)
		{
			_base.Merge(ref other._base);
		}

		public void AddEdge(EdgeCache* cache, EdgeSegment prevEdge, EdgeSegment edge, EdgeSegment nextEdge)
		{
			if (_base.IsEdgeRelevant(*cache, edge, _p))
			{
				double param;
				var dist = edge.SignedDistance(_p, out param);
				_base.AddEdgeTrueDistance(edge, dist, param);
				cache->Point = _p;
				cache->AbsDistance = Math.Abs(dist.Distance);

				Vector2 ap = _p - edge.Point(0);
				Vector2 bp = _p - edge.Point(1);
				Vector2 aDir = edge.Direction(0).Normalize(true);
				Vector2 bDir = edge.Direction(1).Normalize(true);
				Vector2 prevDir = prevEdge.Direction(1).Normalize(true);
				Vector2 nextDir = nextEdge.Direction(0).Normalize(true);

				double add = Vector2.Dot(ap, (prevDir + aDir).Normalize(true));
				double bdd = -Vector2.Dot(bp, (bDir + nextDir).Normalize(true));

				if (add > 0)
				{
					double pd = dist.Distance;
					if (PerpendicularDistanceSelectorBase.GetPerpendicularDistance(ref pd, ap, -aDir))
						_base.AddEdgePerpendicularDistance(-pd);
					cache->APerpDistance = pd;
				}
				if (bdd > 0)
				{
					double pd = dist.Distance;
					if (PerpendicularDistanceSelectorBase.GetPerpendicularDistance(ref pd, bp, bDir))
						_base.AddEdgePerpendicularDistance(pd);
					cache->BPerpDistance = pd;
				}
				cache->ADomainDistance = add;
				cache->BDomainDistance = bdd;
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public double ComputeDistance(Vector2 p) => _base.ComputeDistance(p);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public SignedDistance TrueDistance() => _base.TrueDistance();
	}

	public ref struct PerpendicularDistanceSelectorBase
	{
		private SignedDistance _minTrueDistance;
		private double _minNegPerp, _minPosPerp;
		private EdgeSegment _nearEdge;
		private double _nearEdgeParam;

		public const double DISTANCE_DELTA_FACTOR = 1.001;

		public PerpendicularDistanceSelectorBase()
		{
			_minTrueDistance = new SignedDistance();
			_minNegPerp = -Math.Abs(_minTrueDistance.Distance);
			_minPosPerp = Math.Abs(_minTrueDistance.Distance);
			_nearEdgeParam = 0;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
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

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void Reset(double delta)
		{
			_minTrueDistance.Distance += Arithmetic.NonZeroSign(_minTrueDistance.Distance) * delta;
			_minNegPerp = -Math.Abs(_minTrueDistance.Distance);
			_minPosPerp = Math.Abs(_minTrueDistance.Distance);
			_nearEdgeParam = 0;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
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

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal void AddEdgeTrueDistance(EdgeSegment edge, SignedDistance dist, double param)
		{
			if (dist < _minTrueDistance)
			{
				_minTrueDistance = dist;
				_nearEdge = edge;
				_nearEdgeParam = param;
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal void AddEdgePerpendicularDistance(double d)
		{
			if (d <= 0 && d > _minNegPerp) _minNegPerp = d;
			if (d >= 0 && d < _minPosPerp) _minPosPerp = d;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void Merge(ref PerpendicularDistanceSelectorBase other)
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
			if (_nearEdge.Type != EdgeSegmentType.None)
			{
				var sd = _minTrueDistance;
				_nearEdge.DistanceToPerpendicularDistance(ref sd, p, _nearEdgeParam);
				if (Math.Abs(sd.Distance) < Math.Abs(best))
					best = sd.Distance;
			}
			return best;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public SignedDistance TrueDistance() => _minTrueDistance;
	}

	public ref struct MultiDistanceSelector
	{
		private Vector2 _p;
		private PerpendicularDistanceSelectorBase _r;
		private PerpendicularDistanceSelectorBase _g;
		private PerpendicularDistanceSelectorBase _b;

		public MultiDistanceSelector()
		{
			_p = default;
			_r = new PerpendicularDistanceSelectorBase();
			_g = new PerpendicularDistanceSelectorBase();
			_b = new PerpendicularDistanceSelectorBase();
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
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
			bool doR = (color & EdgeColor.Red) != 0 && _r.IsEdgeRelevant(*cache, edge, _p);
			bool doG = (color & EdgeColor.Green) != 0 && _g.IsEdgeRelevant(*cache, edge, _p);
			bool doB = (color & EdgeColor.Blue) != 0 && _b.IsEdgeRelevant(*cache, edge, _p);

			if (doR || doG || doB)
			{
				double param;
				var dist = edge.SignedDistance(_p, out param);
				if (doR) _r.AddEdgeTrueDistance(edge, dist, param);
				if (doG) _g.AddEdgeTrueDistance(edge, dist, param);
				if (doB) _b.AddEdgeTrueDistance(edge, dist, param);

				cache->Point = _p;
				cache->AbsDistance = Math.Abs(dist.Distance);

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
					cache->APerpDistance = pd;
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
					cache->BPerpDistance = pd;
				}
				cache->ADomainDistance = add;
				cache->BDomainDistance = bdd;
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void Merge(ref MultiDistanceSelector other)
		{
			_r.Merge(ref other._r);
			_g.Merge(ref other._g);
			_b.Merge(ref other._b);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public MultiDistance Distance()
		{
			return new MultiDistance
			{
				R = _r.ComputeDistance(_p),
				G = _g.ComputeDistance(_p),
				B = _b.ComputeDistance(_p)
			};
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public SignedDistance TrueDistance()
		{
			var d = _r.TrueDistance();
			if (_g.TrueDistance() < d) d = _g.TrueDistance();
			if (_b.TrueDistance() < d) d = _b.TrueDistance();
			return d;
		}
	}
}