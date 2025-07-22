using System;
using Typography.OpenFont;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using static System.Formats.Asn1.AsnWriter;
using NumericsVector2 = System.Numerics.Vector2;
using static System.Net.Mime.MediaTypeNames;

namespace Msdfgen.IO
{
    public static class ImportFont
    {

        public static Typeface LoadFont(string filename)
        {
            using FileStream file = File.OpenRead(filename);
            OpenFontReader reader = new OpenFontReader();
            return reader.Read(file);
        }

        public static void DestroyFont(Typeface font)
        {
            
        }

        public static double GetFontScale(Typeface font)
        {
            return font.UnitsPerEm / 64.0;
        }

        public static void GetFontWhitespaceWidth(ref double spaceAdvance, ref double tabAdvance, Typeface font)
        {
            ushort glyphIdx;
            glyphIdx = font.GetGlyphIndex(' ');
            spaceAdvance = font.GetAdvanceWidthFromGlyphIndex(glyphIdx) / 64.0;
            glyphIdx = font.GetGlyphIndex('\t');
            tabAdvance = font.GetAdvanceWidthFromGlyphIndex(glyphIdx) / 64.0;
        }

        //public static Shape LoadGlyph(Typeface font, uint unicode, ref double advance)
        //{
        //    var result = new Shape();
        //    var glyphIdx = font.GetGlyphIndex((int)unicode);
        //    var glyphIdx = font.GetGlyph(glyphIdx); // TODO: Figure out how to get from unicode
        //    result.InverseYAxis = false;
        //    advance = font.GetAdvanceWidthFromGlyphIndex(glyphIdx) / 64.0;
        //    var context = new FtContext(result);

        //    //var ftFunctions = new OutlineFuncs
        //    //{
        //    //    MoveFunction = context.FtMoveTo,
        //    //    LineFunction = context.FtLineTo,
        //    //    ConicFunction = context.FtConicTo,
        //    //    CubicFunction = context.FtCubicTo,
        //    //    Shift = 0
        //    //};

        //    return result;
        //}
        /// <summary>
        /// Loads a glyph from a Typography Typeface into an Msdfgen.Shape,
        /// returning its advance (in the same 1/64 units) and the ideal
        /// bitmap width/height for MSDF generation.
        /// Only TrueType outlines (glyf table) are supported.
        /// </summary>
        public static Shape LoadGlyph(
        Typeface typeface,
        uint unicode,
        ref double advance,
        out int bitmapWidth,
        out int bitmapHeight)
        {
            ushort glyphIndex = (ushort)typeface.GetGlyphIndex((int)unicode);
            var glyph = typeface.GetGlyph(glyphIndex);
            if (glyph == null)
            {
                advance = 0;
                bitmapWidth = bitmapHeight = 0;
                return new Shape();
            }

            int advUnits = typeface.GetAdvanceWidthFromGlyphIndex(glyphIndex);
            advance = advUnits / 64.0;

            const int padding = 2;      // pixels

            // 1) Raw glyph bounds in font units
            var bounds = glyph.Bounds;
            double wUnits = bounds.XMax - bounds.XMin;
            double hUnits = bounds.YMax - bounds.YMin;

            // 2) Scale factor (1/64 for 26.6 → float)
            double scale = 1.0 / 64.0;

            // 3) Compute padded bitmap dimensions
            bitmapWidth = (int)Math.Ceiling(wUnits * scale) + padding * 2;
            bitmapHeight = (int)Math.Ceiling(hUnits * scale) + padding * 2;

            // 4) Compute offset so that glyph’s bottom‐left maps to (padding, padding)
            double offsetX = -bounds.XMin * scale + padding;
            double offsetY = -bounds.YMin * scale + padding;

            Shape shape = new Shape();
            GlyphPointF[] pts = glyph.GlyphPoints;
            ushort[] ends = glyph.EndPoints;
            int start = 0;

            (double X, double Y) ToShapeSpace(GlyphPointF p)
                => (p.X * scale + offsetX, p.Y * scale + offsetY);

            foreach (ushort end in ends)
            {
                int count = end - start + 1;
                if (count <= 0) { start = end + 1; continue; }
                var contourPts = new List<GlyphPointF>(count);
                for (int i = start; i <= end; i++)
                    contourPts.Add(pts[i]);

                Contour contour = new Contour();

                bool firstOff = !contourPts[0].onCurve;
                GlyphPointF firstPt = firstOff
                    ? new GlyphPointF(
                        (contourPts[^1].X + contourPts[0].X) * 0.5f,
                        (contourPts[^1].Y + contourPts[0].Y) * 0.5f,
                        true)
                    : contourPts[0];

                var currentOn = firstPt;
                GlyphPointF? pendingOff = null;
                int idx0 = firstOff ? 0 : 1;

                for (int i = idx0; i < contourPts.Count; i++)
                {
                    var pt = contourPts[i];
                    if (pt.onCurve)
                    {
                        if (pendingOff.HasValue)
                        {
                            var c0 = ToShapeSpace(currentOn);
                            var c1 = ToShapeSpace(pendingOff.Value);
                            var c2 = ToShapeSpace(pt);
                            contour.Add(new QuadraticSegment(
                                EdgeColor.White,
                                new Vector2((float)c0.X, (float)c0.Y),
                                new Vector2((float)c1.X, (float)c1.Y),
                                new Vector2((float)c2.X, (float)c2.Y)
                            ));
                            pendingOff = null;
                            currentOn = pt;
                        }
                        else
                        {
                            var c0 = ToShapeSpace(currentOn);
                            var c1 = ToShapeSpace(pt);
                            contour.Add(new LinearSegment(
                                new Vector2((float)c0.X, (float)c0.Y),
                                new Vector2((float)c1.X, (float)c1.Y)
                            ));
                            currentOn = pt;
                        }
                    }
                    else
                    {
                        if (!pendingOff.HasValue)
                        {
                            pendingOff = pt;
                        }
                        else
                        {
                            var lastOff = pendingOff.Value;
                            GlyphPointF implied = new GlyphPointF(
                                (lastOff.X + pt.X) * 0.5f,
                                (lastOff.Y + pt.Y) * 0.5f,
                                true);

                            var c0 = ToShapeSpace(currentOn);
                            var c1 = ToShapeSpace(lastOff);
                            var c2 = ToShapeSpace(implied);
                            contour.Add(new QuadraticSegment(
                                EdgeColor.White,
                                new Vector2((float)c0.X, (float)c0.Y),
                                new Vector2((float)c1.X, (float)c1.Y),
                                new Vector2((float)c2.X, (float)c2.Y)
                            ));

                            currentOn = implied;
                            pendingOff = pt;
                        }
                    }
                }

                // Close
                if (pendingOff.HasValue)
                {
                    var c0 = ToShapeSpace(currentOn);
                    var c1 = ToShapeSpace(pendingOff.Value);
                    var c2 = ToShapeSpace(firstPt);
                    contour.Add(new QuadraticSegment(
                        EdgeColor.White,
                        new Vector2((float)c0.X, (float)c0.Y),
                        new Vector2((float)c1.X, (float)c1.Y),
                        new Vector2((float)c2.X, (float)c2.Y)
                    ));
                }
                else
                {
                    var c0 = ToShapeSpace(currentOn);
                    var c1 = ToShapeSpace(firstPt);
                    contour.Add(new LinearSegment(
                        new Vector2((float)c0.X, (float)c0.Y),
                        new Vector2((float)c1.X, (float)c1.Y)
                    ));
                }

                shape.Add(contour);
                start = end + 1;
            }

            return shape;
        }

        //private static Contour GenerateContour(Typeface font, Glyph glyphIdx, Shape shape, int start, int end)
        //{
        //    Contour contour = new Contour();
        //    shape.Add(contour);
        //    GlyphPointF[] points = glyphIdx.GlyphPoints;
        //    double scale = font.UnitsPerEm;

        //    int count = end - start + 1;
        //    List<GlyphPointF> contourPoints = new ( points[start..(end + 1)] );

        //    // Handle wraparound: add first point to end
        //    contourPoints.Add(contourPoints[0]);

        //    int eP = 0;
        //    while (eP < contourPoints.Count - 1)
        //    {
        //        var curr = contourPoints[eP];
        //        var next = contourPoints[eP + 1];

        //        Vector2 pos = ToVec(curr, scale);
        //        Vector2 nextPos = ToVec(next, scale);

        //        if (curr.onCurve && next.onCurve)
        //        {
        //            contour.Add(new LinearSegment(pos, nextPos));
        //            eP++;
        //        }
        //        else if (curr.onCurve && !next.onCurve)
        //        {
        //            var nextNext = contourPoints[(eP + 2) % contourPoints.Count];
        //            Vector2 nextNextPos = ToVec(nextNext, scale);

        //            if (nextNext.onCurve)
        //            {
        //                contour.Add(new QuadraticSegment(EdgeColor.White, pos, ToVec(next, scale), nextNextPos));
        //                eP += 2;
        //            }
        //            else
        //            {
        //                // Insert implied on-curve between next and nextNext
        //                Vector2 mid = (ToVec(next, scale) + ToVec(nextNext, scale)) * 0.5f;

        //                contour.Add(new QuadraticSegment(EdgeColor.White, pos, ToVec(next, scale), mid));
        //                eP += 1;

        //                // Treat mid as new current for next iteration
        //                contourPoints.Insert(eP + 1, new GlyphPointF {  P = mid, onCurve = true });
        //            }
        //        }
        //        else
        //        {
        //            // Should never hit: contours always start on-curve or insert implied
        //            eP++;
        //        }
        //    }
        //    return contour;
        //}

        private static Vector2 ToVec(GlyphPointF pt, double scale) =>
            new (pt.P.X / scale, pt.P.Y / scale);

        public static double GetKerning(Typeface font, uint unicode1, uint unicode2)
        {
            var kerning = font.GetKernDistance(0, 1); // TODO: Figure out how to get from unicode
            return kerning / 64.0;
        }



        //private class FtContext
        //{
        //    private readonly Shape _shape;
        //    private Contour _contour;
        //    private Vector2 _position;

        //    public FtContext(Shape output)
        //    {
        //        _shape = output;
        //    }

        //    private static Vector2 FtPoint2(ref Vector2 vector)
        //    {
        //        return new Vector2(vector.X / 64.0, vector.Y / 64.0);
        //    }

        //    internal int FtMoveTo(ref Vector2 to, IntPtr context)
        //    {
        //        _contour = new Contour();
        //        _shape.Add(_contour);
        //        _position = FtPoint2(ref to);
        //        return 0;
        //    }

        //    internal int FtLineTo(ref Vector2 to, IntPtr context)
        //    {
        //        _contour.Add(new LinearSegment(_position, FtPoint2(ref to)));
        //        _position = FtPoint2(ref to);
        //        return 0;
        //    }

        //    internal int FtConicTo(ref Vector2 control, ref Vector2 to, IntPtr context)
        //    {
        //        _contour.Add(new QuadraticSegment(EdgeColor.White,_position, FtPoint2(ref control), FtPoint2(ref to)));
        //        _position = FtPoint2(ref to);
        //        return 0;
        //    }

        //    internal int FtCubicTo(ref Vector2 control1, ref Vector2 control2, ref Vector2 to, IntPtr context)
        //    {
        //        _contour.Add(new CubicSegment(EdgeColor.White, _position, FtPoint2(ref control1), FtPoint2(ref control2),FtPoint2(ref to)));
        //        _position = FtPoint2(ref to);
        //        return 0;
        //    }
        //}
    }
}