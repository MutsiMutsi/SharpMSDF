using System;
using Typography.OpenFont;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using static System.Formats.Asn1.AsnWriter;
using NumericsVector2 = System.Numerics.Vector2;
using static System.Net.Mime.MediaTypeNames;
using SharpMSDF.Core;

namespace SharpMSDF.IO
{
    public enum FontCoordinateScaling
    {
        None,
        EmNormalized,
        LegacyNormalized
    }

    public static class FontImporter
    {

        public static Typeface LoadFont(string filename)
        {
            using FileStream file = File.OpenRead(filename);
            OpenFontReader reader = new OpenFontReader();
            return reader.Read(file);
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

        /// <summary>
        /// Loads a glyph from a Typography Typeface into a <see cref="Shape"/>,
        /// returning its advance (in the same 1/64 units) and the ideal
        /// Only TrueType outlines (glyf table) are supported.
        /// </summary>
        public static Shape LoadGlyph(
            Typeface typeface,
            uint unicode,
            FontCoordinateScaling scaling)
        {
            return LoadGlyph(typeface, unicode, scaling, out _, out _);
        }

        /// <summary>
        /// Loads a glyph from a Typography Typeface into a <see cref="Shape"/>,
        /// returning its advance (in the same 1/64 units) and the ideal
        /// Only TrueType outlines (glyf table) are supported.
        /// </summary>
        public static Shape LoadGlyph(
            Typeface typeface,
            uint unicode,
            FontCoordinateScaling scaling,
            out float idealWidth,
            out float idealHeight
            )
        {
            double adv = 0f;
            return LoadGlyph(typeface, unicode, scaling, out idealWidth, out idealHeight, ref adv);
        }

        /// <summary>
        /// Loads a glyph from a Typography Typeface into a <see cref="Shape"/>,
        /// returning its advance (in the same 1/64 units) and the ideal
        /// Only TrueType outlines (glyf table) are supported.
        /// </summary>
        public static Shape LoadGlyph(
            Typeface typeface,
            uint unicode,
            FontCoordinateScaling scaling,
            out float idealWidth,
            out float idealHeight,
            ref double advance
            )
        {
            //const double scale = 1.0 / 64;
            ushort glyphIndex = (ushort)typeface.GetGlyphIndex((int)unicode);
            var glyph = typeface.GetGlyph(glyphIndex);
            if (glyph == null)
            {
                advance = 0;
                idealWidth = idealHeight = 0;
                return new Shape();
            }

            int advUnits = typeface.GetAdvanceWidthFromGlyphIndex(glyphIndex);
            advance = advUnits / 64;

            //const int padding = 0;      // pixels

            // 1) Raw glyph bounds in font units
            var bounds = glyph.Bounds;
            double wUnits = bounds.XMax - bounds.XMin;
            double hUnits = bounds.YMax - bounds.YMin;

            // 2) Compute padded bitmap dimensions
            idealWidth = (float)wUnits / 64f; // + padding * 2;
            idealHeight = (float)hUnits / 64f; // + padding * 2;

            // 3) Compute offset so that glyph’s bottom‐left maps to (padding, padding)
            double offsetX = -bounds.XMin / 64; // + padding
            double offsetY = -bounds.YMin / 64; // + padding

            Shape shape = new Shape();
            GlyphPointF[] pts = glyph.GlyphPoints;
            ushort[] ends = glyph.EndPoints;
            int start = 0;

            double divW = 1.0;
            double divH = 1.0;
            if (scaling == FontCoordinateScaling.EmNormalized)
            {
                divW = wUnits;
                divH = hUnits;
            }
            else if (scaling == FontCoordinateScaling.LegacyNormalized)
            {
                divW = 64;
                divH = 64;
            }

            (double X, double Y) ToShapeSpace(GlyphPointF p)
                => ((p.X + offsetX )/ divW, (p.Y + offsetY) / divH);

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
                            contour.Edges.Add(new QuadraticSegment(
                                new Vector2((float)c0.X, (float)c0.Y),
                                new Vector2((float)c1.X, (float)c1.Y),
                                new Vector2((float)c2.X, (float)c2.Y),
                                EdgeColor.White
                            ));
                            pendingOff = null;
                            currentOn = pt;
                        }
                        else
                        {
                            var c0 = ToShapeSpace(currentOn);
                            var c1 = ToShapeSpace(pt);
                            contour.Edges.Add(new LinearSegment(
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
                            contour.Edges.Add(new QuadraticSegment(
                                new Vector2((float)c0.X, (float)c0.Y),
                                new Vector2((float)c1.X, (float)c1.Y),
                                new Vector2((float)c2.X, (float)c2.Y),
                                EdgeColor.White
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
                    contour.Edges.Add(new QuadraticSegment(
                        new Vector2((float)c0.X, (float)c0.Y),
                        new Vector2((float)c1.X, (float)c1.Y),
                        new Vector2((float)c2.X, (float)c2.Y),
                        EdgeColor.White
                    ));
                }
                else
                {
                    var c0 = ToShapeSpace(currentOn);
                    var c1 = ToShapeSpace(firstPt);
                    contour.Edges.Add(new LinearSegment(
                        new Vector2((float)c0.X, (float)c0.Y),
                        new Vector2((float)c1.X, (float)c1.Y)
                    ));
                }

                shape.Contours.Add(contour);
                start = end + 1;
            }

            return shape;
        }


        private static Vector2 ToVec(GlyphPointF pt, double scale) =>
            new (pt.P.X / scale, pt.P.Y / scale);

        public static double GetKerning(Typeface font, uint unicode1, uint unicode2)
        {
            var kerning = font.GetKernDistance(0, 1); // TODO: Figure out how to get from unicode
            return kerning / 64.0;
        }

    }
}