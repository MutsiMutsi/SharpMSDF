
using System.ComponentModel;
using System.Numerics;

namespace SharpMSDF.Core
{
	public static class EdgeColorings
	{
		private const int MSDFGEN_EDGE_LENGTH_PRECISION = 4;
		private const int MAX_RECOLOR_STEPS = 16;
		private const int EDGE_DISTANCE_PRECISION = 16;

		private static int SymmetricalTrichotomy(int position, int n)
		{
			return (int)(3 + 2.875f * position / (n - 1) - 1.4375f + 0.5f) - 3;
		}

		private static bool IsCorner(Vector2 aDir, Vector2 bDir, float crossThreshold)
		{
			return Vector2.Dot(aDir, bDir) <= 0 || Math.Abs(VectorExtensions.Cross(aDir, bDir)) > crossThreshold;
		}

		private static float EstimateEdgeLength(EdgeSegment edge)
		{
			float len = 0;
			Vector2 prev = edge.Point(0);
			for (int i = 1; i <= MSDFGEN_EDGE_LENGTH_PRECISION; ++i)
			{
				Vector2 cur = edge.Point((1.0f / MSDFGEN_EDGE_LENGTH_PRECISION) * i);
				len += (cur - prev).Length();
				prev = cur;
			}
			return len;
		}

		private static int SeedExtract2(ref ulong seed)
		{
			int v = (int)seed & 1;
			seed >>= 1;
			return v;
		}

		private static int SeedExtract3(ref ulong seed)
		{
			int v = (int)(seed % 3);
			seed /= 3;
			return v;
		}

		private static EdgeColor InitColor(ref ulong seed)
		{
			Span<EdgeColor> colors = [EdgeColor.Cyan, EdgeColor.Magenta, EdgeColor.Yellow];
			return colors[SeedExtract3(ref seed)];
		}

		private static void SwitchColor(ref EdgeColor color, ref ulong seed)
		{
			int shifted = (int)color << (1 + SeedExtract2(ref seed));
			color = (EdgeColor)((shifted | (shifted >> 3)) & (int)EdgeColor.White);
		}

		private static void SwitchColor(ref EdgeColor color, ref ulong seed, EdgeColor banned)
		{
			EdgeColor combined = color & banned;
			if (combined == EdgeColor.Red || combined == EdgeColor.Green || combined == EdgeColor.Blue)
				color = (EdgeColor)((int)combined ^ (int)EdgeColor.White);
			else
				SwitchColor(ref color, ref seed);
		}

		/*public static void Simple(Shape shape, float angleThreshold, ulong seed = 0)
		{
			float crossThreshold = MathF.Sin(angleThreshold);
			EdgeColor color = InitColor(ref seed);
			List<int> corners = new();
			EdgeSegment[] parts = new EdgeSegment[7];

			foreach (var contour in shape.Contours)
			{
				if (contour.Edges.Count == 0)
					continue;

				// Identify corners
				corners.Clear();
				Vector2 prevDirection = contour.Edges[^1].Direction(1);
				for (int i = 0; i < contour.Edges.Count; i++)
				{
					if (IsCorner(Vector2.Normalize(prevDirection), Vector2.Normalize(contour.Edges[i].Direction(0)), crossThreshold))
						corners.Add(i);
					prevDirection = contour.Edges[i].Direction(1);
				}

				if (corners.Count == 0)
				{
					SwitchColor(ref color, ref seed);

					for (int i = 0; i < contour.Edges.Count; i++)
					{
						contour.Edges[i] = contour.Edges[i].WithColor(color);
					}

				}
				else if (corners.Count == 1)
				{
					EdgeColor[] colors = new EdgeColor[3];
					SwitchColor(ref color, ref seed);
					colors[0] = color;
					colors[1] = EdgeColor.White;
					SwitchColor(ref color, ref seed);
					colors[2] = color;
					int corner = corners[0];

					if (contour.Edges.Count >= 3)
					{
						int m = contour.Edges.Count;
						for (int i = 0; i < m; ++i)
							contour.Edges[(corner + i) % m] = contour.Edges[(corner + i) % m].WithColor(colors[1 + SymmetricalTrichotomy(i, m)]);
					}
					else if (contour.Edges.Count >= 1)
					{
						contour.Edges[0].SplitInThirds(
							out parts[0 + 3 * corner],
							out parts[1 + 3 * corner],
							out parts[2 + 3 * corner]);

						if (contour.Edges.Count >= 2)
						{
							contour.Edges[1].SplitInThirds(
								out parts[3 - 3 * corner],
								out parts[4 - 3 * corner],
								out parts[5 - 3 * corner]);

							parts[0] = parts[0].WithColor(colors[0]);
							parts[1] = parts[1].WithColor(colors[0]);
							parts[2] = parts[2].WithColor(colors[1]);
							parts[3] = parts[3].WithColor(colors[1]);
							parts[4] = parts[4].WithColor(colors[2]);
							parts[5] = parts[5].WithColor(colors[2]);
						}
						else
						{
							parts[0] = parts[0].WithColor(colors[0]);
							parts[1] = parts[1].WithColor(colors[1]);
							parts[2] = parts[2].WithColor(colors[2]);
						}

						contour.Edges.Clear();
						for (int p = 0; p < parts.Length; p++)
						{
							if(parts[p].Type == EdgeSegmentType.None) {
								throw new Exception("THIS SHOULD NOT HAPPEN");
							}
							EdgeSegment part = parts[p];
							//TODO: REMOVED NULL CHECK if (part != null)
								contour.Edges.Add(part);
						}
					}
				}
				else
				{
					int cornerCount = corners.Count;
					int spline = 0;
					int start = corners[0];
					int m = contour.Edges.Count;
					SwitchColor(ref color, ref seed);
					EdgeColor initialColor = color;

					for (int i = 0; i < m; ++i)
					{
						int index = (start + i) % m;
						if (spline + 1 < cornerCount && corners[spline + 1] == index)
						{
							spline++;
							SwitchColor(ref color, ref seed, (EdgeColor)((spline == cornerCount - 1) ? (int)initialColor : 0));
						}
						contour.Edges[index] = contour.Edges[index].WithColor(color);
					}
				}
			}
		}*/

		private struct InkTrapCorner
		{
			public int Index;
			public float PrevEdgeLengthEstimate;
			public bool Minor;
			public EdgeColor Color;
		}

		[ThreadStatic]
		private static List<InkTrapCorner> corners = new();

		public static void InkTrap(Shape shape, float angleThreshold, ulong seed)
		{
			float crossThreshold = MathF.Sin(angleThreshold);
			EdgeColor color = InitColor(ref seed);
			Span<EdgeColor> colors = stackalloc EdgeColor[3];

			for (int ci = 0; ci < shape.ContourCount; ci++)
			{
				var contour = shape.Contours[ci];
				if (contour.Count == 0)
					continue;

				float splineLength = 0;
				corners.Clear();

				// Get the last edge's direction for the loop
				Vector2 prevDirection = shape.Edges[contour.Start + contour.Count - 1].Direction(1);

				for (int e = 0; e < contour.Count; e++)
				{
					var edge = shape.Edges[contour.Start + e];
					if (IsCorner(Vector2.Normalize(prevDirection), Vector2.Normalize(edge.Direction(0)), crossThreshold))
					{
						corners.Add(new InkTrapCorner
						{
							Index = e,
							PrevEdgeLengthEstimate = splineLength
						});
						splineLength = 0;
					}

					splineLength += EstimateEdgeLength(edge);
					prevDirection = edge.Direction(1);
				}

				if (corners.Count == 0)
				{
					SwitchColor(ref color, ref seed);
					for (int e = 0; e < contour.Count; e++)
						shape.Edges[contour.Start + e] = shape.Edges[contour.Start + e].WithColor(color);
				}
				else if (corners.Count == 1)
				{
					// One-corner ("teardrop") case
					SwitchColor(ref color, ref seed);
					colors[0] = color;
					colors[1] = EdgeColor.White;
					SwitchColor(ref color, ref seed);
					colors[2] = color;

					int cornerIndex = corners[0].Index;

					if (contour.Count >= 3)
					{
						int m = contour.Count;
						for (int i = 0; i < m; ++i)
						{
							int colorIndex = 1 + SymmetricalTrichotomy(i, m);
							int edgeIndex = contour.Start + (cornerIndex + i) % m;
							shape.Edges[edgeIndex] = shape.Edges[edgeIndex].WithColor(colors[colorIndex]);
						}
					}
					else if (contour.Count >= 1)
					{
						// Split edges and assign colors
						EdgeSegment a1, a2, a3;
						EdgeSegment b1 = new(), b2 = new(), b3 = new();

						shape.Edges[contour.Start].SplitInThirds(out a1, out a2, out a3);
						if (contour.Count >= 2)
							shape.Edges[contour.Start + 1].SplitInThirds(out b1, out b2, out b3);

						a1 = a1.WithColor(colors[0]);
						a2 = a2.WithColor(colors[0]);
						a3 = a3.WithColor(colors[1]);

						if (b1.Type != EdgeSegmentType.None)
						{
							b1 = b1.WithColor(colors[1]);
							b2 = b2.WithColor(colors[2]);
							b3 = b3.WithColor(colors[2]);
						}

						// Remove original edges and insert new ones
						// Note: This is tricky because we're modifying the edges list while iterating
						// We need to handle this carefully to maintain contour integrity

						// First, collect all new edges
						var newEdges = new List<EdgeSegment> { a1, a2, a3 };
						if (b1.Type != EdgeSegmentType.None)
							newEdges.AddRange(new[] { b1, b2, b3 });

						// Remove old edges (in reverse order to maintain indices)
						for (int i = Math.Min(contour.Count, 2) - 1; i >= 0; i--)
							shape.Edges.RemoveAt(contour.Start + i);

						// Insert new edges
						for (int i = 0; i < newEdges.Count; i++)
							shape.Edges.Insert(contour.Start + i, newEdges[i]);

						// Update this contour's count
						var updatedContour = contour;
						updatedContour.Count = newEdges.Count;
						shape.Contours[ci] = updatedContour;

						// Update start indices for all subsequent contours
						int deltaEdges = newEdges.Count - Math.Min(contour.Count, 2);
						for (int k = ci + 1; k < shape.ContourCount; k++)
						{
							var laterContour = shape.Contours[k];
							laterContour.Start += deltaEdges;
							shape.Contours[k] = laterContour;
						}
					}
				}
				else
				{
					// Multiple corners
					int cornerCount = corners.Count;
					int majorCornerCount = cornerCount;

					// Detect minor corners
					if (cornerCount > 3)
					{
						var first = corners[0];
						first.PrevEdgeLengthEstimate += splineLength;
						corners[0] = first;

						for (int i = 0; i < cornerCount; i++)
						{
							float a = corners[i].PrevEdgeLengthEstimate;
							float b = corners[(i + 1) % cornerCount].PrevEdgeLengthEstimate;
							float c = corners[(i + 2) % cornerCount].PrevEdgeLengthEstimate;

							if (a > b && b < c)
							{
								var corner = corners[i];
								corner.Minor = true;
								majorCornerCount--;
								corners[i] = corner;
							}
						}
					}

					EdgeColor initialColor = EdgeColor.Black;
					for (int i = 0; i < cornerCount; i++)
					{
						if (!corners[i].Minor)
						{
							majorCornerCount--;
							SwitchColor(ref color, ref seed, majorCornerCount == 0 ? initialColor : 0);
							var corner = corners[i];
							corner.Color = color;
							corners[i] = corner;

							if (initialColor == EdgeColor.Black)
								initialColor = color;
						}
					}

					for (int i = 0; i < cornerCount; i++)
					{
						if (corners[i].Minor)
						{
							EdgeColor nextColor = corners[(i + 1) % cornerCount].Color;
							var corner = corners[i];
							corner.Color = (EdgeColor)(((int)color & (int)nextColor) ^ (int)EdgeColor.White);
							corners[i] = corner;
						}
						else
						{
							color = corners[i].Color;
						}
					}

					// Assign colors along the edges
					int spline = 0;
					int start = corners[0].Index;
					color = corners[0].Color;
					int m = contour.Count;

					for (int i = 0; i < m; ++i)
					{
						int localIndex = (start + i) % m;
						int globalIndex = contour.Start + localIndex;
						if (spline + 1 < cornerCount && corners[spline + 1].Index == localIndex)
							color = corners[++spline].Color;

						shape.Edges[globalIndex] = shape.Edges[globalIndex].WithColor(color);
					}
				}
			}
		}
	}
}