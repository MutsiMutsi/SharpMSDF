using System.Numerics;
using System.Runtime.CompilerServices;

namespace SharpMSDF.Core
{
	public struct MultiDistance
	{
		public float R, G, B;
	}

	public struct EdgeCache
	{
		public Vector2 Point;
		public float AbsDistance;
		public float ADomainDistance, BDomainDistance;
		public float APerpDistance, BPerpDistance;
	}

	public unsafe ref struct PerpendicularDistanceSelector
	{
		private Vector2 _p;
		private PerpendicularDistanceSelectorBase _base;

		public const float DISTANCE_DELTA_FACTOR = 1.001f;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void Reset(Vector2 p)
		{
			float delta = DISTANCE_DELTA_FACTOR * (p - _p).Length();
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
				SignedDistance dist = edge.SignedDistance(_p, out float param);
				_base.AddEdgeTrueDistance(edge, dist, param);
				cache->Point = _p;
				cache->AbsDistance = MathF.Abs(dist.Distance);

				Vector2 ap = _p - edge.Point(0);
				Vector2 bp = _p - edge.Point(1);
				Vector2 aDir = Vector2.Normalize(edge.Direction(0));
				Vector2 bDir = Vector2.Normalize(edge.Direction(1));
				Vector2 prevDir = Vector2.Normalize(prevEdge.Direction(1));
				Vector2 nextDir = Vector2.Normalize(nextEdge.Direction(0));

				float add = Vector2.Dot(ap, Vector2.Normalize(prevDir + aDir));
				float bdd = -Vector2.Dot(bp, Vector2.Normalize(bDir + nextDir));

				if (add > 0)
				{
					float pd = dist.Distance;
					if (PerpendicularDistanceSelectorBase.GetPerpendicularDistance(ref pd, ap, -aDir))
					{
						_base.AddEdgePerpendicularDistance(-pd);
					}

					cache->APerpDistance = pd;
				}
				if (bdd > 0)
				{
					float pd = dist.Distance;
					if (PerpendicularDistanceSelectorBase.GetPerpendicularDistance(ref pd, bp, bDir))
					{
						_base.AddEdgePerpendicularDistance(pd);
					}

					cache->BPerpDistance = pd;
				}
				cache->ADomainDistance = add;
				cache->BDomainDistance = bdd;
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public float ComputeDistance(Vector2 p)
		{
			return _base.ComputeDistance(p);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public SignedDistance TrueDistance()
		{
			return _base.TrueDistance();
		}
	}

	public ref struct PerpendicularDistanceSelectorBase
	{
		private SignedDistance _minTrueDistance;
		private float _minNegPerp, _minPosPerp;
		private EdgeSegment _nearEdge;
		private float _nearEdgeParam;

		public const float DISTANCE_DELTA_FACTOR = 1.001f;

		public PerpendicularDistanceSelectorBase()
		{
			_minTrueDistance = new SignedDistance();
			_minNegPerp = -Math.Abs(_minTrueDistance.Distance);
			_minPosPerp = Math.Abs(_minTrueDistance.Distance);
			_nearEdgeParam = 0;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool GetPerpendicularDistance(ref float distance, Vector2 ep, Vector2 edgeDir)
		{
			float ts = Vector2.Dot(ep, edgeDir);
			if (ts > 0)
			{
				float perp = VectorExtensions.Cross(ep, edgeDir);
				if (Math.Abs(perp) < Math.Abs(distance))
				{
					distance = perp;
					return true;
				}
			}
			return false;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void Reset(float delta)
		{
			_minTrueDistance.Distance += Arithmetic.NonZeroSign(_minTrueDistance.Distance) * delta;
			_minNegPerp = -Math.Abs(_minTrueDistance.Distance);
			_minPosPerp = Math.Abs(_minTrueDistance.Distance);
			_nearEdgeParam = 0;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public bool IsEdgeRelevant(EdgeCache cache, EdgeSegment edge, Vector2 p)
		{
			float delta = DISTANCE_DELTA_FACTOR * (p - cache.Point).Length();
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
		internal void AddEdgeTrueDistance(EdgeSegment edge, SignedDistance dist, float param)
		{
			if (dist < _minTrueDistance)
			{
				_minTrueDistance = dist;
				_nearEdge = edge;
				_nearEdgeParam = param;
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal void AddEdgePerpendicularDistance(float d)
		{
			if (d <= 0 && d > _minNegPerp)
			{
				_minNegPerp = d;
			}

			if (d >= 0 && d < _minPosPerp)
			{
				_minPosPerp = d;
			}
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
			if (other._minNegPerp > _minNegPerp)
			{
				_minNegPerp = other._minNegPerp;
			}

			if (other._minPosPerp < _minPosPerp)
			{
				_minPosPerp = other._minPosPerp;
			}
		}

		internal float ComputeDistance(Vector2 p)
		{
			float best = _minTrueDistance.Distance < 0 ? _minNegPerp : _minPosPerp;
			if (_nearEdge.Type != EdgeSegmentType.None)
			{
				SignedDistance sd = _minTrueDistance;
				_nearEdge.DistanceToPerpendicularDistance(ref sd, p, _nearEdgeParam);
				if (Math.Abs(sd.Distance) < Math.Abs(best))
				{
					best = sd.Distance;
				}
			}
			return best;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public SignedDistance TrueDistance()
		{
			return _minTrueDistance;
		}
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
			float delta = PerpendicularDistanceSelectorBase.DISTANCE_DELTA_FACTOR * (p - _p).Length();
			_r.Reset(delta);
			_g.Reset(delta);
			_b.Reset(delta);
			_p = p;
		}

		public unsafe void AddEdge(EdgeCache* cache, EdgeSegment prev, EdgeSegment edge, EdgeSegment next)
		{
			EdgeColor color = edge.Color;
			int flags = 0;
			if ((color & EdgeColor.Red) != 0 && _r.IsEdgeRelevant(*cache, edge, _p))
			{
				flags |= 1;
			}

			if ((color & EdgeColor.Green) != 0 && _g.IsEdgeRelevant(*cache, edge, _p))
			{
				flags |= 2;
			}

			if ((color & EdgeColor.Blue) != 0 && _b.IsEdgeRelevant(*cache, edge, _p))
			{
				flags |= 4;
			}

			if (flags == 0)
			{
				return;
			}

			SignedDistance dist = edge.SignedDistance(_p, out float param);
			float absDist = MathF.Abs(dist.Distance);

			if ((flags & 1) != 0)
			{
				_r.AddEdgeTrueDistance(edge, dist, param);
			}

			if ((flags & 2) != 0)
			{
				_g.AddEdgeTrueDistance(edge, dist, param);
			}

			if ((flags & 4) != 0)
			{
				_b.AddEdgeTrueDistance(edge, dist, param);
			}

			cache->Point = _p;
			cache->AbsDistance = absDist;

			Vector2 ap = _p - edge.Point(0);
			Vector2 bp = _p - edge.Point(1);

			Vector2 aDir = edge.Direction(0); ////Normalisation removed...
			Vector2 bDir = edge.Direction(1); //Normalisation removed...
			Vector2 prevDir = prev.Direction(1); //Normalisation removed...
			Vector2 nextDir = next.Direction(0); //Normalisation removed...

			Vector2 aSum = prevDir + aDir;
			Vector2 bSum = bDir + nextDir;

			float add = Vector2.Dot(ap, aSum); //Normalisation removed...
			float bdd = -Vector2.Dot(bp, bSum); //Normalisation removed...

			cache->ADomainDistance = add;
			cache->BDomainDistance = bdd;

			if (add > 0)
			{
				float pd = dist.Distance;
				if (PerpendicularDistanceSelectorBase.GetPerpendicularDistance(ref pd, ap, -aDir))
				{
					pd = -pd;
					if ((flags & 1) != 0)
					{
						_r.AddEdgePerpendicularDistance(pd);
					}

					if ((flags & 2) != 0)
					{
						_g.AddEdgePerpendicularDistance(pd);
					}

					if ((flags & 4) != 0)
					{
						_b.AddEdgePerpendicularDistance(pd);
					}
				}
				cache->APerpDistance = pd;
			}

			if (bdd > 0)
			{
				float pd = dist.Distance;
				if (PerpendicularDistanceSelectorBase.GetPerpendicularDistance(ref pd, bp, bDir))
				{
					if ((flags & 1) != 0)
					{
						_r.AddEdgePerpendicularDistance(pd);
					}

					if ((flags & 2) != 0)
					{
						_g.AddEdgePerpendicularDistance(pd);
					}

					if ((flags & 4) != 0)
					{
						_b.AddEdgePerpendicularDistance(pd);
					}
				}
				cache->BPerpDistance = pd;
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
			SignedDistance d = _r.TrueDistance();
			if (_g.TrueDistance() < d)
			{
				d = _g.TrueDistance();
			}

			if (_b.TrueDistance() < d)
			{
				d = _b.TrueDistance();
			}

			return d;
		}
	}
}