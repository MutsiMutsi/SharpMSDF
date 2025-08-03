
using System.ComponentModel;

namespace SharpMSDF.Core
{
    public static class EdgeColorings
    {
        private const int MSDFGEN_EDGE_LENGTH_PRECISION = 4;
        private const int MAX_RECOLOR_STEPS = 16;
        private const int EDGE_DISTANCE_PRECISION = 16;

        private static int SymmetricalTrichotomy(int position, int n)
        {
            return (int)(3 + 2.875 * position / (n - 1) - 1.4375 + 0.5) - 3;
        }

        private static bool IsCorner(Vector2 aDir, Vector2 bDir, double crossThreshold)
        {
            return Vector2.Dot(aDir, bDir) <= 0 || Math.Abs(Vector2.Cross(aDir, bDir)) > crossThreshold;
        }

        private static double EstimateEdgeLength(EdgeSegment edge)
        {
            double len = 0;
            Vector2 prev = edge.Point(0);
            for (int i = 1; i <= MSDFGEN_EDGE_LENGTH_PRECISION; ++i)
            {
                Vector2 cur = edge.Point((1.0 / MSDFGEN_EDGE_LENGTH_PRECISION) * i);
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
            Span<EdgeColor> colors = [ EdgeColor.Cyan, EdgeColor.Magenta, EdgeColor.Yellow ];
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

        public static void Simple(Shape shape, double angleThreshold, ulong seed = 0)
        {
            double crossThreshold = Math.Sin(angleThreshold);
            EdgeColor color = InitColor(ref seed);
            List<int> corners = new();
            EdgeSegment[] parts = new EdgeSegment[7];

            foreach (var contour in shape.Contours)
            {
                if (contour.Edges.Count == 0)
                    continue;

                // Identify corners
                corners.Clear();
                Vector2 prevDirection = contour.Edges[^1].Segment.Direction(1);
                for (int i = 0; i < contour.Edges.Count; i++)
                {
                    if (IsCorner(prevDirection.Normalize(), contour.Edges[i].Segment.Direction(0).Normalize(), crossThreshold))
                        corners.Add(i);
                    prevDirection = contour.Edges[i].Segment.Direction(1);
                }

                if (corners.Count == 0)
                {
                    SwitchColor(ref color, ref seed);
                    foreach (var edge in contour.Edges)
                        edge.Segment.Color = color;
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
                            contour.Edges[(corner + i) % m].Segment.Color = colors[1 + SymmetricalTrichotomy(i, m)];
                    }
                    else if (contour.Edges.Count >= 1)
                    {
                        contour.Edges[0].Segment.SplitInThirds(out parts[0 + 3 * corner], out parts[1 + 3 * corner], out parts[2 + 3 * corner]);
                        if (contour.Edges.Count >= 2)
                        {
                            contour.Edges[1].Segment.SplitInThirds(out parts[3 - 3 * corner], out parts[4 - 3 * corner], out parts[5 - 3 * corner]);
                            parts[0].Color = parts[1].Color = colors[0];
                            parts[2].Color = parts[3].Color = colors[1];
                            parts[4].Color = parts[5].Color = colors[2];
                        }
                        else
                        {
                            parts[0].Color = colors[0];
                            parts[1].Color = colors[1];
                            parts[2].Color = colors[2];
                        }

                        contour.Edges.Clear();
                        for (int p = 0; p < parts.Length; p++)
                        {
                            EdgeSegment? part = parts[p];
                            if (part != null)
                                contour.Edges.Add(new EdgeHolder(part));
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
                        contour.Edges[index].Segment.Color = color;
                    }
                }
            }
        }

        private struct InkTrapCorner
        {
            public int Index;
            public double PrevEdgeLengthEstimate;
            public bool Minor;
            public EdgeColor Color;
        }

        public static void InkTrap(Shape shape, double angleThreshold, ulong seed = 0)
        {
            double crossThreshold = Math.Sin(angleThreshold);
            EdgeColor color = InitColor(ref seed);
            List<InkTrapCorner> corners = [];

            Span<EdgeColor> colors = stackalloc EdgeColor[3];

            for (int ctr = 0; ctr < shape.Contours.Count; ctr++)
            {
                Contour contour = shape.Contours[ctr];
                if (contour.Edges.Count == 0)
                    continue;

                double splineLength = 0;
                corners.Clear();

                Vector2 prevDirection = contour.Edges[^1].Segment.Direction(1);
                for (int e = 0; e < contour.Edges.Count; e++)
                {
                    var edge = contour.Edges[e];
                    if (IsCorner(prevDirection.Normalize(), edge.Segment.Direction(0).Normalize(), crossThreshold))
                    {
                        corners.Add(new InkTrapCorner
                        {
                            Index = e,
                            PrevEdgeLengthEstimate = splineLength
                        });
                        splineLength = 0;
                    }

                    splineLength += EstimateEdgeLength(edge);
                    prevDirection = edge.Segment.Direction(1);
                }

                if (corners.Count == 0)
                {
                    SwitchColor(ref color, ref seed);
                    for (int e = 0; e < contour.Edges.Count; e++)
                        contour.Edges[e].Segment.Color = color;
                }
                else if (corners.Count == 1)
                {
                    SwitchColor(ref color, ref seed);
                    colors[0] = color;
                    colors[1] = EdgeColor.White;
                    SwitchColor(ref color, ref seed);
                    colors[2] = color;

                    int corner = corners[0].Index;
                    if (contour.Edges.Count >= 3)
                    {
                        int m = contour.Edges.Count;
                        for (int i = 0; i < m; ++i)
                            contour.Edges[(corner + i) % m].Segment.Color = colors[1 + SymmetricalTrichotomy(i, m)];
                    }
                    else if (contour.Edges.Count >= 1)
                    {
                        EdgeSegment[] parts = new EdgeSegment[7];
                        contour.Edges[0].Segment.SplitInThirds(out parts[0 + 3 * corner], out parts[1 + 3 * corner], out parts[2 + 3 * corner]);
                        if (contour.Edges.Count >= 2)
                        {
                            contour.Edges[1].Segment.SplitInThirds(out parts[3 - 3 * corner], out parts[4 - 3 * corner], out parts[5 - 3 * corner]);
                            parts[0].Color = parts[1].Color = colors[0];
                            parts[2].Color = parts[3].Color = colors[1];
                            parts[4].Color = parts[5].Color = colors[2];
                        }
                        else
                        {
                            parts[0].Color = colors[0];
                            parts[1].Color = colors[1];
                            parts[2].Color = colors[2];
                        }

                        contour.Edges.Clear();
                        foreach (var part in parts)
                        {
                            if (part != null)
                                contour.Edges.Add(new EdgeHolder(part));
                        }
                    }
                }
                else
                {
                    InkTrapCorner corner;
                    int cornerCount = corners.Count;
                    int majorCornerCount = cornerCount;

                    if (cornerCount > 3)
                    {
                        corner = corners[0];
                        corner.PrevEdgeLengthEstimate += splineLength;
                        corners[0] = corner;
                        for (int i = 0; i < cornerCount; i++)
                        {
                            double a = corners[i].PrevEdgeLengthEstimate;
                            double b = corners[(i + 1) % cornerCount].PrevEdgeLengthEstimate;
                            double c = corners[(i + 2) % cornerCount].PrevEdgeLengthEstimate;

                            if (a > b && b < c)
                            {
                                corner = corners[i];
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
                            SwitchColor(ref color, ref seed, majorCornerCount==0 ? initialColor : 0);
                            corner = corners[i];
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
                            corner = corners[i];
                            corner.Color = (EdgeColor)((int)(color & nextColor) ^ (int)EdgeColor.White);
                            corners[i] = corner;
                        }
                        else
                        {
                            color = corners[i].Color;
                        }
                    }

                    int spline = 0;
                    int start = corners[0].Index;
                    color = corners[0].Color;
                    int m = contour.Edges.Count;

                    for (int i = 0; i < m; ++i)
                    {
                        int index = (start + i) % m;
                        if (spline + 1 < cornerCount && corners[spline + 1].Index == index)
                            color = corners[++spline].Color;
                        contour.Edges[index].Segment.Color = color;
                    }
                }
            }
        }
    }
}