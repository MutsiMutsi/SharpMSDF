using SharpMSDF.Core;
using System.Numerics;
using System.Runtime.InteropServices;

public struct Shape
{
	public const float MSDFGEN_CORNER_DOT_EPSILON = .000001f;

	public struct Bounds
	{
		public float l, b, r, t;
	}

	public struct ContourRange
	{
		public int Start;
		public int Count;
	}

	public List<EdgeSegment> Edges;
	public List<ContourRange> Contours;
	public bool InverseYAxis;

	private int _currentContourIndex;

	readonly static float _Ratio = 0.5f * (MathF.Sqrt(5) - 1);

	public int CurrentContourIdx => _currentContourIndex;

	public Shape()
	{
		Edges = new List<EdgeSegment>();
		Contours = new List<ContourRange>();
		InverseYAxis = false;
		_currentContourIndex = -1;
	}

	// ---- Contour/Edge API ----

	public void StartContour()
	{
		// Close the previous contour if any
		if (_currentContourIndex >= 0)
		{
			var cr = Contours[_currentContourIndex];
			cr.Count = Edges.Count - cr.Start;
			Contours[_currentContourIndex] = cr;
		}

		_currentContourIndex = Contours.Count;
		Contours.Add(new ContourRange
		{
			Start = Edges.Count,
			Count = 0
		});
	}

	public void AddEdge(EdgeSegment edge)
	{
		if (_currentContourIndex < 0)
			StartContour();

		Edges.Add(edge);
		var cr = Contours[_currentContourIndex];
		cr.Count++;
		Contours[_currentContourIndex] = cr;
	}

	public ReadOnlySpan<EdgeSegment> GetContourEdges(int contourIndex)
	{
		var cr = Contours[contourIndex];
		return CollectionsMarshal.AsSpan(Edges).Slice(cr.Start, cr.Count);
	}

	public int ContourCount => Contours.Count;

	// ---- Core Methods ----

	public bool Validate()
	{
		for (int ci = 0; ci < Contours.Count; ci++)
		{
			var cr = Contours[ci];
			if (cr.Count > 0)
			{
				var corner = Edges[cr.Start + cr.Count - 1].Point(1);
				for (int j = 0; j < cr.Count; j++)
				{
					var edge = Edges[cr.Start + j];

					if (edge.Type == EdgeSegmentType.None)
						throw new Exception("THIS SHOULD NOT HAPPEN");

					if (edge.Point(0) != corner)
						return false;
					corner = edge.Point(1);
				}
			}
		}
		return true;
	}

	public void Normalize()
	{
		for (int ci = 0; ci < Contours.Count; ci++)
		{
			var cr = Contours[ci];
			if (cr.Count == 1)
			{
				Edges[cr.Start].SplitInThirds(out var part0, out var part1, out var part2);
				Edges[cr.Start] = part0;
				Edges.Insert(cr.Start + 1, part1);
				Edges.Insert(cr.Start + 2, part2);

				// Adjust contour count
				cr.Count = 3;
				Contours[ci] = cr;

				// Adjust later contour start indices
				for (int k = ci + 1; k < Contours.Count; k++)
				{
					var later = Contours[k];
					later.Start += 2; // two new edges inserted
					Contours[k] = later;
				}
			}
			else
			{
				EdgeSegment prevEdge = Edges[cr.Start + cr.Count - 1];
				for (int i = 0; i < cr.Count; i++)
				{
					EdgeSegment edge = Edges[cr.Start + i];
					Vector2 prevDir = Vector2.Normalize(prevEdge.Direction(1));
					Vector2 curDir = Vector2.Normalize(edge.Direction(0));
					if (Vector2.Dot(prevDir, curDir) < MSDFGEN_CORNER_DOT_EPSILON - 1)
					{
						float factor = 1.11111111111111111f *
									   MathF.Sqrt(1 - MathF.Pow(MSDFGEN_CORNER_DOT_EPSILON - 1, 2)) /
									   (MSDFGEN_CORNER_DOT_EPSILON - 1);
						var axis = factor * Vector2.Normalize(curDir - prevDir);
						if (VectorExtensions.Cross(prevEdge.DirectionChange(1), edge.Direction(0)) +
							VectorExtensions.Cross(edge.DirectionChange(0), prevEdge.Direction(1)) < 0)
							axis = -axis;

						// DeconvergeEdge calls could be restored here if needed
					}
					prevEdge = edge;
				}
			}
		}
	}
	public int GetWinding(int contourIndex)
	{
		var cr = Contours[contourIndex];
		if (cr.Count == 0)
			return 0;

		float total = 0;

		static float Shoelace(Vector2 a, Vector2 b) => (b.X - a.X) * (a.Y + b.Y);

		if (cr.Count == 1)
		{
			Vector2 a = Edges[cr.Start + 0].Point(0);
			Vector2 b = Edges[cr.Start + 0].Point(1.0f / 3.0f);
			Vector2 c = Edges[cr.Start + 0].Point(2.0f / 3.0f);
			total += Shoelace(a, b);
			total += Shoelace(b, c);
			total += Shoelace(c, a);
		}
		else if (cr.Count == 2)
		{
			Vector2 a = Edges[cr.Start + 0].Point(0);
			Vector2 b = Edges[cr.Start + 0].Point(0.5f);
			Vector2 c = Edges[cr.Start + 1].Point(0);
			Vector2 d = Edges[cr.Start + 1].Point(0.5f);
			total += Shoelace(a, b);
			total += Shoelace(b, c);
			total += Shoelace(c, d);
			total += Shoelace(d, a);
		}
		else
		{
			Vector2 prev = Edges[cr.Start + cr.Count - 1].Point(0);
			for (int e = 0; e < cr.Count; e++)
			{
				Vector2 cur = Edges[cr.Start + e].Point(0);
				total += Shoelace(prev, cur);
				prev = cur;
			}
		}

		return Math.Sign(total);
	}

	public void Bound(ref float l, ref float b, ref float r, ref float t)
	{
		for (int ci = 0; ci < Contours.Count; ci++)
		{
			var cr = Contours[ci];
			for (int e = 0; e < cr.Count; e++)
				Edges[cr.Start + e].Bound(ref l, ref b, ref r, ref t);
		}
	}

	public void BoundMiters(ref float l, ref float b, ref float r, ref float t, float border, float miterLimit, int polarity)
	{
		for (int ci = 0; ci < Contours.Count; ci++)
		{
			var cr = Contours[ci];
			if (cr.Count == 0)
				continue;

			Vector2 prevDir = Vector2.Normalize(Edges[cr.Start + cr.Count - 1].Direction(1));

			for (int e = 0; e < cr.Count; e++)
			{
				Vector2 dir = -Vector2.Normalize(Edges[cr.Start + e].Direction(0));
				if (polarity * VectorExtensions.Cross(prevDir, dir) >= 0)
				{
					float miterLength = miterLimit;
					float q = 0.5f * (1f - Vector2.Dot(prevDir, dir));
					if (q > 0)
						miterLength = MathF.Min(1 / MathF.Sqrt(q), miterLimit);

					Vector2 miter = Edges[cr.Start + e].Point(0) +
									(border * miterLength * Vector2.Normalize(prevDir + dir));
					BoundPoint(ref l, ref b, ref r, ref t, miter);
				}
				prevDir = Vector2.Normalize(Edges[cr.Start + e].Direction(1));
			}
		}
	}

	public Bounds GetBounds(float border = 0.0f, float miterLimit = 0.0f, int polarity = 0)
	{
		const float LARGE_VALUE = float.MaxValue;
		var bounds = new Bounds
		{
			l = +LARGE_VALUE,
			b = +LARGE_VALUE,
			r = -LARGE_VALUE,
			t = -LARGE_VALUE
		};
		Bound(ref bounds.l, ref bounds.b, ref bounds.r, ref bounds.t);
		if (border > 0)
		{
			bounds.l -= border; bounds.b -= border;
			bounds.r += border; bounds.t += border;
			if (miterLimit > 0)
				BoundMiters(ref bounds.l, ref bounds.b, ref bounds.r, ref bounds.t, border, miterLimit, polarity);
		}
		return bounds;
	}

	public int EdgeCount() => Edges.Count;

	public void OrientContours()
	{
		var orientations = new int[Contours.Count];
		var intersections = new List<Intersection>();

		Span<float> x = stackalloc float[3];
		Span<int> dy = stackalloc int[3];

		for (int c = 0; c < Contours.Count; ++c)
		{
			var cr = Contours[c];
			if (orientations[c] == 0 && cr.Count > 0)
			{
				float y0 = Edges[cr.Start].Point(0).Y;
				float y1 = y0;
				for (int e = 0; e < cr.Count && y0 == y1; e++)
					y1 = Edges[cr.Start + e].Point(1).Y;
				for (int e = 0; e < cr.Count && y0 == y1; e++)
					y1 = Edges[cr.Start + e].Point(_Ratio).Y;

				float y = Arithmetic.Mix(y0, y1, _Ratio);

				for (int ci = 0; ci < Contours.Count; ++ci)
				{
					var cr2 = Contours[ci];
					for (int ei = 0; ei < cr2.Count; ei++)
					{
						var edge = Edges[cr2.Start + ei];
						int n = edge.ScanlineIntersections(x, dy, y);
						for (int k = 0; k < n; ++k)
							intersections.Add(new Intersection { X = x[k], Direction = dy[k], ContourIndex = ci });
					}
				}

				if (intersections.Count > 0)
				{
					intersections.Sort((a, b) => Math.Sign(a.X - b.X));

					for (int j = 1; j < intersections.Count; ++j)
						if (intersections[j].X == intersections[j - 1].X)
						{
							intersections[j - 1] = intersections[j - 1] with { Direction = 0 };
							intersections[j] = intersections[j] with { Direction = 0 };
						}

					for (int j = 0; j < intersections.Count; ++j)
						if (intersections[j].Direction != 0)
							orientations[intersections[j].ContourIndex] +=
								2 * ((j & 1) ^ (intersections[j].Direction > 0 ? 1 : 0)) - 1;

					intersections.Clear();
				}
			}
		}

		for (int i = 0; i < Contours.Count; ++i)
			if (orientations[i] < 0)
				ReverseContour(i);
	}

	public void ReverseContour(int contourIndex)
	{
		var cr = Contours[contourIndex];
		int count = cr.Count;
		for (int i = 0; i < count / 2; ++i)
		{
			(Edges[cr.Start + i], Edges[cr.Start + count - i - 1]) =
				(Edges[cr.Start + count - i - 1], Edges[cr.Start + i]);
		}
		for (int e = 0; e < count; e++)
			Edges[cr.Start + e] = Edges[cr.Start + e].Reverse();
	}

	// ---- Helpers ----

	private static void BoundPoint(ref float l, ref float b, ref float r, ref float t, Vector2 p)
	{
		if (p.X < l) l = p.X;
		if (p.Y < b) b = p.Y;
		if (p.X > r) r = p.X;
		if (p.Y > t) t = p.Y;
	}

	private struct Intersection
	{
		public float X;
		public int Direction;
		public int ContourIndex;
	}
}
