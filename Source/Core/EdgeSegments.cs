using System.Numerics;
using System.Runtime.CompilerServices;

namespace SharpMSDF.Core
{
	public enum EdgeSegmentType : byte
	{
		None = 0,
		Linear,
		Quadratic,
		//Cubic
	}

	public readonly struct EdgeSegment
	{
		public readonly EdgeSegmentType Type;
		public readonly EdgeColor Color;

		private readonly LinearSegment linear;
		private readonly QuadraticSegment quadratic;
		//private readonly CubicSegment cubic;

		public EdgeSegment(LinearSegment segment, EdgeColor color = EdgeColor.White)
		{
			Type = EdgeSegmentType.Linear;
			Color = color;
			linear = segment;
			quadratic = default;
			//cubic = default;
		}

		public EdgeSegment(QuadraticSegment segment, EdgeColor color = EdgeColor.White)
		{
			Type = EdgeSegmentType.Quadratic;
			Color = color;
			linear = default;
			quadratic = segment;
			//cubic = default;
		}

		/*public EdgeSegment(CubicSegment segment, EdgeColor color = EdgeColor.White)
		{
			Type = EdgeSegmentType.Cubic;
			Color = color;
			linear = default;
			quadratic = default;
			cubic = segment;
		}*/

		public Vector2 Point(float t)
			=> Type switch
			{
				EdgeSegmentType.Linear => linear.Point(t),
				EdgeSegmentType.Quadratic => quadratic.Point(t),
				//EdgeSegmentType.Cubic => cubic.Point(t),
				_ => default
			};

		public Vector2 Direction(float t)
			=> Type switch
			{
				EdgeSegmentType.Linear => linear.Direction(t),
				EdgeSegmentType.Quadratic => quadratic.Direction(t),
				//EdgeSegmentType.Cubic => cubic.Direction(t),
				_ => default
			};

		public SignedDistance SignedDistance(Vector2 origin, out float param)
			=> Type switch
			{
				EdgeSegmentType.Linear => linear.SignedDistance(origin, out param),
				EdgeSegmentType.Quadratic => quadratic.SignedDistance(origin, out param),
				//EdgeSegmentType.Cubic => cubic.SignedDistance(origin, out param),
				_ => throw new InvalidOperationException()
			};

		public void Bound(ref float l, ref float b, ref float r, ref float t)
		{
			switch (Type)
			{
				case EdgeSegmentType.Linear:
					linear.Bound(ref l, ref b, ref r, ref t);
					break;
				case EdgeSegmentType.Quadratic:
					quadratic.Bound(ref l, ref b, ref r, ref t);
					break;
				//case EdgeSegmentType.Cubic:
					//cubic.Bound(ref l, ref b, ref r, ref t);
					//break;
			}
		}

		public void SplitInThirds(out EdgeSegment part0, out EdgeSegment part1, out EdgeSegment part2)
		{
			switch (Type)
			{
				case EdgeSegmentType.Linear:
					linear.SplitInThirds(out part0, out part1, out part2, Color);
					break;
				case EdgeSegmentType.Quadratic:
					quadratic.SplitInThirds(out part0, out part1, out part2, Color);
					break;
				//case EdgeSegmentType.Cubic:
					//cubic.SplitInThirds(out part0, out part1, out part2, Color);
					//break;
				default:
					throw new InvalidOperationException();
			}
		}

		public EdgeSegment Reverse()
		{
			return Type switch
			{
				EdgeSegmentType.Linear => new EdgeSegment(linear.Reverse(), Color),
				EdgeSegmentType.Quadratic => new EdgeSegment(quadratic.Reverse(), Color),
				//EdgeSegmentType.Cubic => new EdgeSegment(cubic.Reverse(), Color),
				_ => this
			};
		}

		public Vector2 DirectionChange(float param)
		{
			switch (Type)
			{
				case EdgeSegmentType.Linear:
					return linear.DirectionChange(param);
				case EdgeSegmentType.Quadratic:
					return quadratic.DirectionChange(param);
				//case EdgeSegmentType.Cubic:
					//return cubic.DirectionChange(param);
				default:
					throw new InvalidOperationException();
			}
		}

		public Vector2[] ControlPoints()
		{
			//TODO: Make these spans!??
			switch (Type)
			{
				case EdgeSegmentType.Linear:
					return [linear.P0, linear.P1];
				case EdgeSegmentType.Quadratic:
					return [quadratic.P0, quadratic.P1, quadratic.P2];
				//case EdgeSegmentType.Cubic:
					//return [cubic.P0, cubic.P1, cubic.P2, cubic.P3];
				default:
					throw new InvalidOperationException();
			}
		}

		public void DistanceToPerpendicularDistance(ref SignedDistance distance, Vector2 origin, float param)
		{
			if (param < 0)
			{
				Vector2 dir = Vector2.Normalize(Direction(0));
				Vector2 aq = origin - Point(0);
				float ts = Vector2.Dot(aq, dir);
				if (ts < 0)
				{
					float perp = VectorExtensions.Cross(aq, dir);
					if (Math.Abs(perp) <= Math.Abs(distance.Distance))
					{
						distance.Distance = perp;
						distance.Dot = 0;
					}
				}
			}
			else if (param > 1)
			{
				Vector2 dir = Vector2.Normalize(Direction(1));
				Vector2 bq = origin - Point(1);
				float ts = Vector2.Dot(bq, dir);
				if (ts > 0)
				{
					float perp = VectorExtensions.Cross(bq, dir);
					if (Math.Abs(perp) <= Math.Abs(distance.Distance))
					{
						distance.Distance = perp;
						distance.Dot = 0;
					}
				}
			}
		}

		public EdgeSegment WithColor(EdgeColor color)
		{
			return Type switch
			{
				EdgeSegmentType.Linear => new EdgeSegment(linear, color),
				EdgeSegmentType.Quadratic => new EdgeSegment(quadratic, color),
				//EdgeSegmentType.Cubic => new EdgeSegment(cubic, color),
				_ => this
			};
		}

		internal QuadraticSegment GetQuadratic()
		{
			return quadratic;
		}

		/*internal CubicSegment GetCubic()
		{
			return cubic;
		}*/

		public int ScanlineIntersections(Span<float> x, Span<int> dy, float y)
		{
			switch (Type)
			{
				case EdgeSegmentType.Linear:
					return linear.ScanlineIntersections(x, dy, y);
				case EdgeSegmentType.Quadratic:
					return quadratic.ScanlineIntersections(x, dy, y);
				/*case EdgeSegmentType.Cubic:
					return cubic.ScanlineIntersections(x, dy, y);*/
				default:
					throw new InvalidOperationException();
			}
		}
	}

	public struct LinearSegment
	{
		public Vector2 P0;
		public Vector2 P1;

		public LinearSegment(Vector2 p0, Vector2 p1)
		{
			P0 = p0;
			P1 = p1;
		}

		public Vector2 Point(float t) => Vector2.Lerp(P0, P1, t);
		public Vector2 Direction(float _) => P1 - P0;

		// Ultra-optimized version with minimal sqrt calls:
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public SignedDistance SignedDistance(Vector2 origin, out float param)
		{
			Vector2 aq = origin - P0;
			Vector2 ab = P1 - P0;

			float abSq = ab.X * ab.X + ab.Y * ab.Y;
			param = (aq.X * ab.X + aq.Y * ab.Y) / abSq;

			float cross = aq.X * ab.Y - aq.Y * ab.X;

			float endpointDist;
			float sign;
			float eqSq;
			Vector2 eq;
			float pseudoDist;

			if (param > 0 && param < 1)
			{
				float abLength = MathF.Sqrt(abSq);
				float orthoDist = cross / abLength;

				// Use squared distance comparison when possible
				eq = (param > 0.5f) ? P1 - origin : P0 - origin;
				eqSq = eq.X * eq.X + eq.Y * eq.Y;

				if (orthoDist * orthoDist < eqSq)
					return new SignedDistance(orthoDist, 0);

				// Endpoint is closer
				endpointDist = MathF.Sqrt(eqSq);
				sign = cross >= 0 ? 1.0f : -1.0f;
				pseudoDist = MathF.Abs(ab.X * eq.X + ab.Y * eq.Y) / (abLength * endpointDist);
				return new SignedDistance(sign * endpointDist, pseudoDist);
			}

			// Outside segment
			eq = (param > 0.5f) ? P1 - origin : P0 - origin;
			eqSq = eq.X * eq.X + eq.Y * eq.Y;
			endpointDist = MathF.Sqrt(eqSq);
			sign = cross >= 0 ? 1.0f : -1.0f;
			pseudoDist = MathF.Abs(ab.X * eq.X + ab.Y * eq.Y) / (MathF.Sqrt(abSq) * endpointDist);

			return new SignedDistance(sign * endpointDist, pseudoDist);
		}

		public void Bound(ref float l, ref float b, ref float r, ref float t)
		{
			if (P0.X < l)
			{
				l = P0.X;
			}

			if (P0.Y < b)
			{
				b = P0.Y;
			}

			if (P0.X > r)
			{
				r = P0.X;
			}

			if (P0.Y > t)
			{
				t = P0.Y;
			}

			if (P1.X < l)
			{
				l = P1.X;
			}

			if (P1.Y < b)
			{
				b = P1.Y;
			}

			if (P1.X > r)
			{
				r = P1.X;
			}

			if (P1.Y > t)
			{
				t = P1.Y;
			}
		}

		public void SplitInThirds(out EdgeSegment part0, out EdgeSegment part1, out EdgeSegment part2, EdgeColor color)
		{
			part0 = new EdgeSegment(new LinearSegment(P0, Point(1.0f / 3.0f)), color);
			part1 = new EdgeSegment(new LinearSegment(Point(1.0f / 3.0f), Point(2.0f / 3.0f)), color);
			part2 = new EdgeSegment(new LinearSegment(Point(2.0f / 3.0f), P1), color);
		}

		public LinearSegment Reverse()
		{
			return new LinearSegment(P1, P0);
		}

		public Vector2 DirectionChange(float t)
		{
			return new Vector2 { X = 0, Y = 0 };
		}

		public int ScanlineIntersections(Span<float> x, Span<int> dy, float y)
		{
			if ((y >= P0.Y && y < P1.Y) || (y >= P1.Y && y < P0.Y))
			{
				float param = (y - P0.Y) / (P1.Y - P0.Y);
				x[0] = Arithmetic.Mix(P0.X, P1.X, param);
				dy[0] = float.Sign(P1.Y - P0.Y);
				return 1;
			}
			return 0;
		}
	}

	public struct QuadraticSegment
	{
		public Vector2 P0;
		public Vector2 P1;
		public Vector2 P2;

		public QuadraticSegment(Vector2 p0, Vector2 p1, Vector2 p2)
		{
			P0 = p0;
			P1 = p1;
			P2 = p2;
		}

		public Vector2 Point(float t)
		{
			return Vector2.Lerp(Vector2.Lerp(P0, P1, t), Vector2.Lerp(P1, P2, t), t);
		}

		public Vector2 Direction(float t)
		{
			Vector2 tangent = Vector2.Lerp(P1 - P0, P2 - P1, t);
			return tangent.Length() == 0 ? P2 - P0 : tangent;
		}

		public SignedDistance SignedDistance(Vector2 origin, out float param)
		{
			// compute helper vectors
			Vector2 qa = P0 - origin;
			Vector2 ab = P1 - P0;
			Vector2 br = P2 - P1 - ab;

			// cubic coefficients for |Q(param)|² derivative = 0
			float a = Vector2.Dot(br, br);
			float b = 3 * Vector2.Dot(ab, br);
			float c = (2 * Vector2.Dot(ab, ab)) + Vector2.Dot(qa, br);
			float d = Vector2.Dot(qa, ab);

			// solve for param in [0,1]
			Span<float> t = stackalloc float[3];
			int solutions = EquationSolver.SolveCubic(t, a, b, c, d);

			// start by assuming the closest is at param=0 (Point A)
			Vector2 epDir = Direction(0);
			float minDistance = Arithmetic.NonZeroSign(VectorExtensions.Cross(epDir, qa)) * qa.Length();
			param = -Vector2.Dot(qa, epDir) / Vector2.Dot(epDir, epDir);

			// check endpoint B (param=1)
			epDir = Direction(1);
			float distB = new Vector2(P2.X - origin.X, P2.Y - origin.Y).Length();
			if (distB < Math.Abs(minDistance))
			{
				minDistance = Arithmetic.NonZeroSign(VectorExtensions.Cross(epDir, new Vector2(P2.X - origin.X, P2.Y - origin.Y))) * distB;
				param = Vector2.Dot(new Vector2(origin.X - P1.X, origin.Y - P1.Y), epDir)
						/ Vector2.Dot(epDir, epDir);
			}

			// check interior critical points
			for (int i = 0; i < solutions; ++i)
			{
				if (t[i] is > 0 and < 1)
				{
					// Q(param) = qa + 2t·ab + param²·br
					Vector2 qe = new(
						qa.X + (2 * t[i] * ab.X) + (t[i] * t[i] * br.X),
						qa.Y + (2 * t[i] * ab.Y) + (t[i] * t[i] * br.Y)
					);
					float dist = qe.Length();
					if (dist <= Math.Abs(minDistance))
					{
						minDistance = Arithmetic.NonZeroSign(VectorExtensions.Cross(ab + (t[i] * br), qe)) * dist;
						param = t[i];
					}
				}
			}

			// choose return form depending on where the closest param lies
			if (param is >= 0 and <= 1)
			{
				return new SignedDistance(minDistance, 0);
			}
			else if (param < 0.5)
			{
				Vector2 dir0 = Vector2.Normalize(Direction(0));
				return new SignedDistance(
					minDistance,
					Math.Abs(Vector2.Dot(dir0, Vector2.Normalize(qa)))
				);
			}
			else
			{
				Vector2 dir1 = Vector2.Normalize(Direction(1));
				Vector2 bq = Vector2.Normalize(new Vector2(P2.X - origin.X, P2.Y - origin.Y));
				return new SignedDistance(
					minDistance,
					Math.Abs(Vector2.Dot(dir1, bq))
				);
			}
		}

		public void Bound(ref float l, ref float b, ref float r, ref float t)
		{
			PointBounds(P0, ref l, ref b, ref r, ref t);
			PointBounds(P2, ref l, ref b, ref r, ref t);
			Vector2 bot = P1 - P0 - (P2 - P1);
			if (bot.X != 0)
			{
				float param = (P1.X - P0.X) / bot.X;
				if (param is > 0 and < 1)
				{
					PointBounds(Point(param), ref l, ref b, ref r, ref t);
				}
			}
			if (bot.Y != 0)
			{
				float param = (P1.Y - P0.Y) / bot.Y;
				if (param is > 0 and < 1)
				{
					PointBounds(Point(param), ref l, ref b, ref r, ref t);
				}
			}
		}

		private static void PointBounds(Vector2 p, ref float l, ref float b, ref float r, ref float t)
		{
			if (p.X < l)
			{
				l = p.X;
			}

			if (p.Y < b)
			{
				b = p.Y;
			}

			if (p.X > r)
			{
				r = p.X;
			}

			if (p.Y > t)
			{
				t = p.Y;
			}
		}

		public void SplitInThirds(out EdgeSegment part0, out EdgeSegment part1, out EdgeSegment part2, EdgeColor color)
		{
			part0 = new EdgeSegment(new QuadraticSegment(P0, Vector2.Lerp(P0, P1, 1 / 3.0f), Point(1f / 3.0f)), color);
			part1 = new EdgeSegment(new QuadraticSegment(Point(1f / 3.0f), Vector2.Lerp(Vector2.Lerp(P0, P1, 5 / 9.0f), Vector2.Lerp(P1, P2, 4 / 9.0f), .5f), Point(2f / 3.0f)), color);
			part2 = new EdgeSegment(new QuadraticSegment(Point(2f / 3.0f), Vector2.Lerp(P1, P2, 2 / 3.0f), P2), color);
		}

		public QuadraticSegment Reverse()
		{
			return new QuadraticSegment(P2, P1, P0);
		}

		public Vector2 DirectionChange(float t)
		{
			return new Vector2
			{
				X = (P2 - P1).X - (P1 - P0).X,
				Y = (P2 - P1).Y - (P1 - P0).Y
			};
		}

		/*public CubicSegment ConvertToCubic()
		{
			return new CubicSegment(P0,
							 Vector2.Lerp(P0, P1, 2.0f / 3.0f),
							 Vector2.Lerp(P1, P2, 1.0f / 3.0f),
							 P2);
		}*/

		public int ScanlineIntersections(Span<float> x, Span<int> dy, float y)
		{
			int total = 0;
			int nextDY = y > P0.Y ? 1 : -1;
			x[total] = P0.X;
			if (P0.Y == y)
			{
				if (P0.Y < P1.Y || (P0.Y == P1.Y && P0.Y < P2.Y))
				{
					dy[total++] = 1;
				}
				else
				{
					nextDY = 1;
				}
			}
			{
				Vector2 ab = P1 - P0;
				Vector2 br = P2 - P1 - ab;
				Span<float> t = stackalloc float[2];
				int solutions = EquationSolver.SolveQuadratic(t, br.Y, 2 * ab.Y, P0.Y - y);
				// Sort solutions
				if (solutions >= 2 && t[0] > t[1])
				{
					(t[0], t[1]) = (t[1], t[0]);
				}

				for (int i = 0; i < solutions && total < 2; ++i)
				{
					if (t[i] is >= 0 and <= 1)
					{
						x[total] = P0.X + (2 * t[i] * ab.X) + (t[i] * t[i] * br.X);
						if (nextDY * (ab.Y + (t[i] * br.Y)) >= 0)
						{
							dy[total++] = nextDY;
							nextDY = -nextDY;
						}
					}
				}
			}
			if (P2.Y == y)
			{
				if (nextDY > 0 && total > 0)
				{
					--total;
					nextDY = -1;
				}
				if ((P2.Y < P1.Y || (P2.Y == P1.Y && P2.Y < P0.Y)) && total < 2)
				{
					x[total] = P2.X;
					if (nextDY < 0)
					{
						dy[total++] = -1;
						nextDY = 1;
					}
				}
			}
			if (nextDY != (y >= P2.Y ? 1 : -1))
			{
				if (total > 0)
				{
					--total;
				}
				else
				{
					if (Math.Abs(P2.Y - y) < Math.Abs(P0.Y - y))
					{
						x[total] = P2.X;
					}

					dy[total++] = nextDY;
				}
			}
			return total;
		}

	}

	/*public struct CubicSegment
	{
		public const int MSDFGEN_CUBIC_SEARCH_STARTS = 4;
		public const int MSDFGEN_CUBIC_SEARCH_STEPS = 4;


		public Vector2 P0;
		public Vector2 P1;
		public Vector2 P2;
		public Vector2 P3;

		public CubicSegment(Vector2 p0, Vector2 p1, Vector2 p2, Vector2 p3)
		{
			P0 = p0;
			P1 = p1;
			P2 = p2;
			P3 = p3;
		}

		public Vector2 Point(float t)
		{
			Vector2 a = Vector2.Lerp(P0, P1, t);
			Vector2 b = Vector2.Lerp(P1, P2, t);
			Vector2 c = Vector2.Lerp(P2, P3, t);
			Vector2 ab = Vector2.Lerp(a, b, t);
			Vector2 bc = Vector2.Lerp(b, c, t);
			return Vector2.Lerp(ab, bc, t);
		}

		public Vector2 Direction(float t)
		{
			Vector2 ab = Vector2.Lerp(P1 - P0, P2 - P1, t);
			Vector2 bc = Vector2.Lerp(P2 - P1, P3 - P2, t);
			Vector2 tangent = Vector2.Lerp(ab, bc, t);
			return tangent.Length() == 0 ? (t == 0 ? P2 - P0 : P3 - P1) : tangent;
		}

		public SignedDistance SignedDistance(Vector2 origin, out float param)
		{
			Vector2 qa = P0 - origin;
			Vector2 ab = P1 - P0;
			Vector2 br = P2 - P1 - ab;
			Vector2 as_ = P3 - P2 - (P2 - P1) - br;

			Vector2 epDir = Direction(0);
			float minDistance = Arithmetic.NonZeroSign(VectorExtensions.Cross(epDir, qa)) * qa.Length(); // distance from A
			param = -Vector2.Dot(qa, epDir) / Vector2.Dot(epDir, epDir);
			{
				epDir = Direction(1);
				float distance = (P3 - origin).Length(); // distance from B
				if (distance < Math.Abs(minDistance))
				{
					minDistance = Arithmetic.NonZeroSign(VectorExtensions.Cross(epDir, P3 - origin)) * distance;
					param = Vector2.Dot(epDir - (P3 - origin), epDir) / Vector2.Dot(epDir, epDir);
				}
			}
			// Iterative minimum distance search
			for (int i = 0; i <= MSDFGEN_CUBIC_SEARCH_STARTS; ++i)
			{
				float t = (float)i / MSDFGEN_CUBIC_SEARCH_STARTS;
				Vector2 qe = qa + (3 * t * ab) + (3 * t * t * br) + (t * t * t * as_);
				for (int step = 0; step < MSDFGEN_CUBIC_SEARCH_STEPS; ++step)
				{
					// Improve t
					Vector2 d1 = (3 * ab) + (6 * t * br) + (3 * t * t * as_);
					Vector2 d2 = (6 * br) + (6 * t * as_);
					t -= Vector2.Dot(qe, d1) / (Vector2.Dot(d1, d1) + Vector2.Dot(qe, d2));
					if (t is <= 0 or >= 1)
					{
						break;
					}

					qe = qa + (3 * t * ab) + (3 * t * t * br) + (t * t * t * as_);
					float distance = qe.Length();
					if (distance < Math.Abs(minDistance))
					{
						minDistance = Arithmetic.NonZeroSign(VectorExtensions.Cross(d1, qe)) * distance;
						param = t;
					}
				}
			}

			if (param is >= 0 and <= 1)
			{
				return new SignedDistance(minDistance, 0);
			}

			return param < .5
				? new SignedDistance(minDistance, Math.Abs(Vector2.Dot(Vector2.Normalize(Direction(0)), Vector2.Normalize(qa))))
				: new SignedDistance(minDistance, Math.Abs(Vector2.Dot(Vector2.Normalize(Direction(1)), Vector2.Normalize(P3 - origin))));
		}

		public void Bound(ref float l, ref float b, ref float r, ref float t)
		{
			PointBounds(P0, ref l, ref b, ref r, ref t);
			PointBounds(P3, ref l, ref b, ref r, ref t);
			Vector2 a0 = P1 - P0;
			Vector2 a1 = 2 * (P2 - P1 - a0);
			Vector2 a2 = P3 - (3 * P2) + (3 * P1) - P0;
			Span<float> prms = stackalloc float[2];
			int solutions;
			solutions = EquationSolver.SolveQuadratic(prms, a2.X, a1.X, a0.X);
			for (int i = 0; i < solutions; ++i)
			{
				if (prms[i] is > 0 and < 1)
				{
					PointBounds(Point(prms[i]), ref l, ref b, ref r, ref t);
				}
			}

			solutions = EquationSolver.SolveQuadratic(prms, a2.Y, a1.Y, a0.Y);
			for (int i = 0; i < solutions; ++i)
			{
				if (prms[i] is > 0 and < 1)
				{
					PointBounds(Point(prms[i]), ref l, ref b, ref r, ref t);
				}
			}

			PointBounds(P0, ref l, ref b, ref r, ref t);
		}

		private static void PointBounds(Vector2 p, ref float l, ref float b, ref float r, ref float t)
		{
			if (p.X < l) l = p.X;
			if (p.Y < b) b = p.Y;
			if (p.X > r) r = p.X;
			if (p.Y > t) t = p.Y;
		}

		public void SplitInThirds(out EdgeSegment part0, out EdgeSegment part1, out EdgeSegment part2, EdgeColor color)
		{
			part0 = new EdgeSegment(new CubicSegment(P0, P0 == P1 ? P0 : Vector2.Lerp(P0, P1, 1 / 3.0f), Vector2.Lerp(Vector2.Lerp(P0, P1, 1 / 3.0f), Vector2.Lerp(P1, P2, 1 / 3.0f), 1 / 3.0f), Point(1 / 3.0f)), color);
			part1 = new EdgeSegment(new CubicSegment(Point(1 / 3.0f),
				Vector2.Lerp(Vector2.Lerp(Vector2.Lerp(P0, P1, 1 / 3.0f), Vector2.Lerp(P1, P2, 1 / 3.0f), 1 / 3.0f), Vector2.Lerp(Vector2.Lerp(P1, P2, 1 / 3.0f), Vector2.Lerp(P2, P3, 1 / 3.0f), 1 / 3.0f), 2 / 3.0f),
				Vector2.Lerp(Vector2.Lerp(Vector2.Lerp(P0, P1, 2 / 3.0f), Vector2.Lerp(P1, P2, 2 / 3.0f), 2 / 3.0f), Vector2.Lerp(Vector2.Lerp(P1, P2, 2 / 3.0f), Vector2.Lerp(P2, P3, 2 / 3.0f), 2 / 3.0f), 1 / 3.0f),
				Point(2 / 3.0f)), color);
			part2 = new EdgeSegment(new CubicSegment(Point(2 / 3.0f), Vector2.Lerp(Vector2.Lerp(P1, P2, 2 / 3.0f), Vector2.Lerp(P2, P3, 2 / 3.0f), 2 / 3.0f), P2 == P3 ? P3 : Vector2.Lerp(P2, P3, 2 / 3.0f), P3), color);
		}

		public CubicSegment Reverse()
		{
			return new CubicSegment(P3, P2, P1, P0);
		}

		public Vector2 DirectionChange(float t)
		{
			return Vector2.Lerp(P2 - P1 - (P1 - P0),
				P3 - P2 - (P2 - P1), t);
		}

		public int ScanlineIntersections(Span<float> x, Span<int> dy, float y)
		{
			int total = 0;
			int nextDY = y > P0.Y ? 1 : -1;
			x[total] = P0.X;
			if (P0.Y == y)
			{
				if (P0.Y < P1.Y || (P0.Y == P1.Y && (P0.Y < P2.Y || (P0.Y == P2.Y && P0.Y < P3.Y))))
				{
					dy[total++] = 1;
				}
				else
				{
					nextDY = 1;
				}
			}
			{
				Vector2 ab = P1 - P0;
				Vector2 br = P2 - P1 - ab;
				Vector2 as_ = P3 - P2 - (P2 - P1) - br;
				Span<float> t = stackalloc float[3];
				int solutions = EquationSolver.SolveCubic(t, as_.Y, 3 * br.Y, 3 * ab.Y, P0.Y - y);
				// Sort solutions
				if (solutions >= 2)
				{
					if (t[0] > t[1])
					{
						(t[0], t[1]) = (t[1], t[0]);
					}

					if (solutions >= 3 && t[1] > t[2])
					{
						(t[2], t[1]) = (t[1], t[2]);
						if (t[0] > t[1])
						{
							(t[0], t[1]) = (t[1], t[0]);
						}
					}
				}
				for (int i = 0; i < solutions && total < 3; ++i)
				{
					if (t[i] is >= 0 and <= 1)
					{
						x[total] = P0.X + (3 * t[i] * ab.X) + (3 * t[i] * t[i] * br.X) + (t[i] * t[i] * t[i] * as_.X);
						if (nextDY * (ab.Y + (2 * t[i] * br.Y) + (t[i] * t[i] * as_.Y)) >= 0)
						{
							dy[total++] = nextDY;
							nextDY = -nextDY;
						}
					}
				}
			}
			if (P3.Y == y)
			{
				if (nextDY > 0 && total > 0)
				{
					--total;
					nextDY = -1;
				}
				if ((P3.Y < P2.Y || (P3.Y == P2.Y && (P3.Y < P1.Y || (P3.Y == P1.Y && P3.Y < P0.Y)))) && total < 3)
				{
					x[total] = P3.X;
					if (nextDY < 0)
					{
						dy[total++] = -1;
						nextDY = 1;
					}
				}
			}
			if (nextDY != (y >= P3.Y ? 1 : -1))
			{
				if (total > 0)
				{
					--total;
				}
				else
				{
					if (Math.Abs(P3.Y - y) < Math.Abs(P0.Y - y))
					{
						x[total] = P3.X;
					}

					dy[total++] = nextDY;
				}
			}
			return total;
		}
	}*/
}
