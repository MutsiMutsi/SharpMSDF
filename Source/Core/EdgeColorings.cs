
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
			// Check if angle is sharp enough to be considered a corner
			float dot = Vector2.Dot(aDir, bDir);
			return dot <= 0 || Math.Abs(VectorExtensions.Cross(aDir, bDir)) > crossThreshold;
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
			EdgeColor[] colors = { EdgeColor.Cyan, EdgeColor.Magenta, EdgeColor.Yellow };
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
				color = (EdgeColor)(combined ^ EdgeColor.White);
			else
				SwitchColor(ref color, ref seed);
		}

		private struct InkTrapCorner
		{
			public int Index;
			public float PrevEdgeLengthEstimate;
			public bool Minor;
			public EdgeColor Color;
		}

		[ThreadStatic]
		private static List<InkTrapCorner> corners = new();

		public static void InkTrap(Shape shape, float angleThreshold, ref ulong seed)
		{
			float crossThreshold = MathF.Sin(angleThreshold);
			EdgeColor color = InitColor(ref seed);

			for (int ci = 0; ci < shape.ContourCount; ci++)
			{
				var contour = shape.Contours[ci];
				if (contour.Count == 0)
					continue;

				float splineLength = 0;

				if (corners == null)
				{
					corners = [];
				}
				else
				{
					corners.Clear();
				}

				// Identify corners
				Vector2 prevDirection = shape.Edges[contour.Start + contour.Count - 1].Direction(1);
				int index = 0;

				for (int e = 0; e < contour.Count; e++)
				{
					var edge = shape.Edges[contour.Start + e];
					if (IsCorner(Vector2.Normalize(prevDirection), Vector2.Normalize(edge.Direction(0)), crossThreshold))
					{
						corners.Add(new InkTrapCorner
						{
							Index = index,
							PrevEdgeLengthEstimate = splineLength,
							Minor = false
						});
						splineLength = 0;
					}
					splineLength += EstimateEdgeLength(edge);
					prevDirection = edge.Direction(1);
					index++;
				}

				// Smooth contour - no corners
				if (corners.Count == 0)
				{
					SwitchColor(ref color, ref seed);
					for (int e = 0; e < contour.Count; e++)
					{
						int edgeIndex = contour.Start + e;
						shape.Edges[edgeIndex] = shape.Edges[edgeIndex].WithColor(color);
					}
				}
				// "Teardrop" case - single corner
				else if (corners.Count == 1)
				{
					EdgeColor[] colors = new EdgeColor[3];
					SwitchColor(ref color, ref seed);
					colors[0] = color;
					colors[1] = EdgeColor.White;
					SwitchColor(ref color, ref seed);
					colors[2] = color;

					int corner = corners[0].Index;

					if (contour.Count >= 3)
					{
						int m = contour.Count;
						for (int i = 0; i < m; i++)
						{
							int edgeIndex = contour.Start + (corner + i) % m;
							int colorIndex = 1 + SymmetricalTrichotomy(i, m);
							shape.Edges[edgeIndex] = shape.Edges[edgeIndex].WithColor(colors[colorIndex]);
						}
					}
					else if (contour.Count >= 1)
					{
						// Less than three edge segments for three colors => edges must be split
						EdgeSegment?[] parts = new EdgeSegment?[7];

						// Split first edge
						shape.Edges[contour.Start].SplitInThirds(out EdgeSegment part0, out EdgeSegment part1, out EdgeSegment part2);
						parts[0 + 3 * corner] = part0;
						parts[1 + 3 * corner] = part1;
						parts[2 + 3 * corner] = part2;

						if (contour.Count >= 2)
						{
							// Split second edge
							shape.Edges[contour.Start + 1].SplitInThirds(out EdgeSegment part3, out EdgeSegment part4, out EdgeSegment part5);
							parts[3 - 3 * corner] = part3;
							parts[4 - 3 * corner] = part4;
							parts[5 - 3 * corner] = part5;

							parts[0] = parts[0]?.WithColor(colors[0]);
							parts[1] = parts[1]?.WithColor(colors[0]);
							parts[2] = parts[2]?.WithColor(colors[1]);
							parts[3] = parts[3]?.WithColor(colors[1]);
							parts[4] = parts[4]?.WithColor(colors[2]);
							parts[5] = parts[5]?.WithColor(colors[2]);
						}
						else
						{
							parts[0] = parts[0]?.WithColor(colors[0]);
							parts[1] = parts[1]?.WithColor(colors[1]);
							parts[2] = parts[2]?.WithColor(colors[2]);
						}

						// Replace edges in contour
						var newEdges = new List<EdgeSegment>();
						for (int i = 0; i < 7 && parts[i] != null; i++)
						{
							newEdges.Add(parts[i].Value);
						}

						// Update the shape's edge list
						ReplaceContourEdges(shape, ci, newEdges);
					}
				}
				// Multiple corners
				else
				{
					int cornerCount = corners.Count;
					int majorCornerCount = cornerCount;

					// Detect minor corners
					if (cornerCount > 3)
					{
						var firstCorner = corners[0];
						firstCorner.PrevEdgeLengthEstimate += splineLength;
						corners[0] = firstCorner;

						for (int i = 0; i < cornerCount; i++)
						{
							float a = corners[i].PrevEdgeLengthEstimate;
							float b = corners[(i + 1) % cornerCount].PrevEdgeLengthEstimate;
							float c = corners[(i + 2) % cornerCount].PrevEdgeLengthEstimate;

							if (a > b && b < c)
							{
								var corner = corners[i];
								corner.Minor = true;
								corners[i] = corner;
								majorCornerCount--;
							}
						}
					}

					// Assign colors to major corners
					EdgeColor initialColor = EdgeColor.Black;
					for (int i = 0; i < cornerCount; i++)
					{
						if (!corners[i].Minor)
						{
							majorCornerCount--;
							EdgeColor targetColor = majorCornerCount == 0 ? initialColor : EdgeColor.Black;
							SwitchColor(ref color, ref seed, targetColor);

							var corner = corners[i];
							corner.Color = color;
							corners[i] = corner;

							if (initialColor == EdgeColor.Black)
								initialColor = color;
						}
					}

					// Assign colors to minor corners
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

					for (int i = 0; i < m; i++)
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

		// Helper method to replace edges in a contour while maintaining indices
		private static void ReplaceContourEdges(Shape shape, int contourIndex, List<EdgeSegment> newEdges)
		{
			var contour = shape.Contours[contourIndex];
			int oldCount = contour.Count;
			int newCount = newEdges.Count;
			int delta = newCount - oldCount;

			// Remove old edges (backwards to maintain indices)
			for (int i = oldCount - 1; i >= 0; i--)
			{
				shape.Edges.RemoveAt(contour.Start + i);
			}

			// Insert new edges
			for (int i = 0; i < newCount; i++)
			{
				shape.Edges.Insert(contour.Start + i, newEdges[i]);
			}

			// Update this contour's count
			var updatedContour = contour;
			updatedContour.Count = newCount;
			shape.Contours[contourIndex] = updatedContour;

			// Update start indices for subsequent contours
			for (int ci = contourIndex + 1; ci < shape.ContourCount; ci++)
			{
				var laterContour = shape.Contours[ci];
				laterContour.Start += delta;
				shape.Contours[ci] = laterContour;
			}
		}
	}
}