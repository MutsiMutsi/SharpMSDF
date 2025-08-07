
using System.Numerics;

namespace SharpMSDF.Core
{

	public struct Shape
	{
		// Threshold of the dot product of adjacent edge directions to be considered convergent.
		public const float MSDFGEN_CORNER_DOT_EPSILON = .000001f;

		public struct Bounds
		{
			public float l, b, r, t;
		}

		/// <summary>
		/// The list of contours the Shape consists of.
		/// </summary>
		public List<Contour> Contours = [];
		/// <summary>
		/// Specifies whether the Shape uses bottom-to-top (false) or top-to-bottom (true) Y coordinates.
		/// </summary>
		public bool InverseYAxis = false;

		/// <summary>
		/// Adds a contour.
		/// </summary>
		public void AddContour(Contour contour)
		{
			Contours.Add(contour);
		}

		/// <summary>
		/// Adds a blank contour and returns its reference.
		/// </summary>
		public Contour AddContour()
		{
			var contour = new Contour();
			Contours.Add(contour);
			return contour;
		}

		/// <summary>
		/// Performs basic checks to determine if the object represents a valid Shape.
		/// </summary>
		public bool Validate()
		{
			for (int i = 0; i < Contours.Count; i++)
			{
				var contour = Contours[i];
				if (contour.Edges.Count > 0)
				{
					var corner = contour.Edges[^1].Point(1);
					for (int j = 0; j < contour.Edges.Count; j++)
					{
						var edge = contour.Edges[j];

						if (edge.Type == EdgeSegmentType.None)
						{
							throw new Exception("THIS SHOULD NOT HAPPEN");
						}

						//TODO: we removed null check edge == null || 
						if (edge.Point(0) != corner)
							return false;
						corner = edge.Point(1);
					}
				}
			}
			return true;
		}

		private static void DeconvergeEdge(ref EdgeSegment edgeSegment, int param, Vector2 vector)
		{
			switch (edgeSegment.Type)
			{
				case EdgeSegmentType.Quadratic:
					{
						// Convert to cubic and replace the current segment
						var q = edgeSegment.GetQuadratic();
						var c = q.ConvertToCubic();
						edgeSegment = new EdgeSegment(c, edgeSegment.Color);
						goto case EdgeSegmentType.Cubic;
					}

				case EdgeSegmentType.Cubic:
					{
						// Copy out, modify, and replace
						var c = edgeSegment.GetCubic();
						switch (param)
						{
							case 0:
								c = new CubicSegment(
									c.P0,
									c.P1 + (c.P1 - c.P0).Length() * vector,
									c.P2,
									c.P3
								);
								break;
							case 1:
								c = new CubicSegment(
									c.P0,
									c.P1,
									c.P2 + (c.P2 - c.P3).Length() * vector,
									c.P3
								);
								break;
						}
						edgeSegment = new EdgeSegment(c, edgeSegment.Color);
						break;
					}
			}
		}

		/// <summary>
		/// Normalizes the Shape geometry for distance field generation.
		/// </summary>
		public void Normalize()
		{
			for (int c = 0; c < Contours.Count; c++)
			{
				var contour = Contours[c];
				if (contour.Edges.Count == 1)
				{
					contour.Edges[0].SplitInThirds(out var part0, out var part1, out var part2);
					contour.Edges.Clear();
					contour.Edges.Add(part0);
					contour.Edges.Add(part1);
					contour.Edges.Add(part2);
				}
				else
				{
					EdgeSegment prevEdge = contour.Edges[^1];
					for (int i = 0; i < contour.Edges.Count; i++)
					{
						EdgeSegment edge = contour.Edges[i];
						Vector2 prevDir = Vector2.Normalize(prevEdge.Direction(1));
						Vector2 curDir = Vector2.Normalize(edge.Direction(0));
						if (Vector2.Dot(prevDir, curDir) < MSDFGEN_CORNER_DOT_EPSILON - 1)
						{
							float factor = 1.11111111111111111f * MathF.Sqrt(1 - MathF.Pow(MSDFGEN_CORNER_DOT_EPSILON - 1, 2)) / (MSDFGEN_CORNER_DOT_EPSILON - 1);
							var axis = factor * Vector2.Normalize(curDir - prevDir);
							if (VectorExtensions.Cross(prevEdge.DirectionChange(1), edge.Direction(0)) + VectorExtensions.Cross(edge.DirectionChange(0), prevEdge.Direction(1)) < 0)
								axis = -axis;


							DeconvergeEdge(ref prevEdge, 1, axis.GetOrthogonal(true));
							DeconvergeEdge(ref edge, 0, axis.GetOrthogonal(false));
						}
						prevEdge = edge;
					}
				}
			}
		}

		/// <summary>
		/// Adjusts the bounding box to fit the Shape.
		/// </summary>
		public void Bound(ref float l, ref float b, ref float r, ref float t)
		{
			for (int i = 0; i < Contours.Count; i++)
				Contours[i].Bound(ref l, ref b, ref r, ref t);
		}

		/// <summary>
		/// Adjusts the bounding box to fit the Shape border's mitered corners.
		/// </summary>
		public void BoundMiters(ref float l, ref float b, ref float r, ref float t, float border, float miterLimit, int polarity)
		{
			for (int i = 0; i < Contours.Count; i++)
				Contours[i].BoundMiters(ref l, ref b, ref r, ref t, border, miterLimit, polarity);
		}

		/// <summary>
		/// Computes the minimum bounding box that fits the Shape, optionally with a (mitered) border.
		/// </summary>
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

		/// <summary>
		/// Returns the total number of edge segments
		/// </summary>
		public int EdgeCount()
		{
			int total = 0;
			for (int i = 0; i < Contours.Count; i++)
				total += Contours[i].Edges.Count;
			return total;
		}

		readonly static float _Ratio = 0.5f * (MathF.Sqrt(5) - 1);

		public Shape()
		{
		}

		/// <summary>
		/// Assumes its contours are unoriented (even-odd fill rule). Attempts to orient them to conform to the non-zero winding rule.
		/// </summary>
		public void OrientContours()
		{
			var orientations = new int[Contours.Count];
			var intersections = new List<Intersection>();

			Span<float> x = stackalloc float[3];
			Span<int> dy = stackalloc int[3];

			for (int c = 0; c < Contours.Count; ++c)
			{
				if (orientations[c] == 0 && Contours[c].Edges.Count > 0)
				{
					float y0 = Contours[c].Edges[0].Point(0).Y;
					float y1 = y0;
					for (int e = 0; e < Contours[c].Edges.Count && y0 == y1; e++)
						y1 = Contours[c].Edges[e].Point(1).Y;
					for (int e = 0; e < Contours[c].Edges.Count && y0 == y1; e++)
						y1 = Contours[c].Edges[e].Point(_Ratio).Y;

					float y = Arithmetic.Mix(y0, y1, _Ratio);

					for (int ci = 0; ci < Contours.Count; ++ci)
                    {
                        for (int ei = 0; ei < Contours[ci].Edges.Count; ei++)
                        {
                            var edge = Contours[ci].Edges[ei];
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
								//intersections[e].direction = intersections[e - 1].direction = 0;
								intersections[j - 1] = intersections[j - 1] with { Direction = 0 };
								intersections[j] = intersections[j] with { Direction = 0 };
							}

						for (int j = 0; j < intersections.Count; ++j)
							if (intersections[j].Direction != 0)
								orientations[intersections[j].ContourIndex] += 2 * ((j & 1) ^ (intersections[j].Direction > 0 ? 1 : 0)) - 1;

						intersections.Clear();
					}
				}
			}

			for (int i = 0; i < Contours.Count; ++i)
				if (orientations[i] < 0)
					Contours[i].Reverse();
		}

		private struct Intersection
		{
			public float X;
			public int Direction;
			public int ContourIndex;
		}
	}
}
