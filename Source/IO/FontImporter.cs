﻿using System;
using Typography.OpenFont;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using static System.Formats.Asn1.AsnWriter;
using NumericsVector2 = System.Numerics.Vector2;
using static System.Net.Mime.MediaTypeNames;
using SharpMSDF.Core;
using System.Numerics;

namespace SharpMSDF.IO
{
	public enum FontCoordinateScaling
	{
		None,
		EmNormalized,
		LegacyNormalized
	}

	/// Global metrics of a typeface (in face units).
	public struct FontMetrics
	{
		/// The size of one EM.
		public float EmSize;
		/// The vertical position of the ascender and descender relative to the baseline.
		public float AscenderY, DescenderY;
		/// The vertical difference between consecutive baselines.
		public float LineHeight;
		/// The vertical position and thickness of the underline.
		public float UnderlineY/*, UnderlineThickness*/;
	};

	public static class FontImporter
	{

		public static Typeface LoadFont(string filename)
		{
			using FileStream file = File.OpenRead(filename);
			OpenFontReader reader = new OpenFontReader();
			return reader.Read(file);
		}
		public static float GetFontCoordinateScale(Typeface face, FontCoordinateScaling coordinateScaling)
		{
			switch (coordinateScaling)
			{
				case FontCoordinateScaling.None:
					return 1;
				case FontCoordinateScaling.EmNormalized:
					return 1.0f / (face.UnitsPerEm != 0f ? face.UnitsPerEm : 1.0f);
				case FontCoordinateScaling.LegacyNormalized:
					return 1.0f / 64.0f;
			}
			return 1;
		}


		public static bool GetFontMetrics(out FontMetrics metrics, Typeface face, FontCoordinateScaling coordinateScaling)
		{
			float scale = GetFontCoordinateScale(face, coordinateScaling);
			metrics.EmSize = scale * face.UnitsPerEm;
			metrics.AscenderY = scale * face.Ascender;
			metrics.DescenderY = scale * face.Descender;
			metrics.LineHeight = scale * (face.Ascender - face.Descender + face.LineGap);
			metrics.UnderlineY = scale * face.UnderlinePosition;
			//metrics.UnderlineThickness = not implemented
			return true;
		}

		//public static float GetFontScale(Typeface face)
		//{
		//    return face.UnitsPerEm / 64.0;
		//}

		public static void GetFontWhitespaceWidth(ref float spaceAdvance, ref float tabAdvance, Typeface font)
		{
			ushort glyphIdx;
			glyphIdx = font.GetGlyphIndex(' ');
			spaceAdvance = font.GetAdvanceWidthFromGlyphIndex(glyphIdx) / 64.0f;
			glyphIdx = font.GetGlyphIndex('\t');
			tabAdvance = font.GetAdvanceWidthFromGlyphIndex(glyphIdx) / 64.0f;
		}

		public static Shape LoadGlyph(
			Typeface typeface,
			uint unicode,
			FontCoordinateScaling scaling
			)
		{
			//const float scale = 1.0 / 64;
			ushort glyphIndex = (ushort)typeface.GetGlyphIndex((int)unicode);
			if (glyphIndex == 0)
			{
				return null;
			}
			var glyph = typeface.GetGlyph(glyphIndex);

			int advUnits = typeface.GetAdvanceWidthFromGlyphIndex(glyphIndex);

			//const int padding = 0;      // pixels

			// 1) Raw glyph bounds in face units
			var bounds = glyph.Bounds;
			float wUnits = bounds.XMax - bounds.XMin;
			float hUnits = bounds.YMax - bounds.YMin;

			Shape shape = ShapePool.Rent();
			GlyphPointF[] pts = glyph.GlyphPoints;
			ushort[] ends = glyph.EndPoints;
			int start = 0;

			float scale = GetFontCoordinateScale(typeface, scaling);
			shape.Advance = advUnits * scale;

			//float offsetX = bounds.XMin /*/ div*/; // + padding
			//float offsetY = -bounds.YMin /*/ div*/; // + padding

			(float X, float Y) ToShapeSpace(GlyphPointF p)
				=> ((p.X) * scale, (p.Y) * scale);

			foreach (ushort end in ends)
			{
				int count = end - start + 1;
				if (count <= 0) { start = end + 1; continue; }
				var contourPts = new List<GlyphPointF>(count);
				for (int i = start; i <= end; i++)
					contourPts.Add(pts[i]);

				shape.StartContour();

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


							shape.AddEdge(new EdgeSegment(
								new QuadraticSegment(
									new Vector2((float)c0.X, (float)c0.Y),
									new Vector2((float)c1.X, (float)c1.Y),
									new Vector2((float)c2.X, (float)c2.Y)
								)
							));
							pendingOff = null;
							currentOn = pt;
						}
						else
						{
							var c0 = ToShapeSpace(currentOn);
							var c1 = ToShapeSpace(pt);
							shape.AddEdge(new EdgeSegment(
								new LinearSegment(
								new Vector2((float)c0.X, (float)c0.Y),
								new Vector2((float)c1.X, (float)c1.Y)
								)
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

							shape.AddEdge(new EdgeSegment(
								new QuadraticSegment(
								new Vector2((float)c0.X, (float)c0.Y),
								new Vector2((float)c1.X, (float)c1.Y),
								new Vector2((float)c2.X, (float)c2.Y)
								)
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


					shape.AddEdge(new EdgeSegment(
						new QuadraticSegment(
						new Vector2((float)c0.X, (float)c0.Y),
						new Vector2((float)c1.X, (float)c1.Y),
						new Vector2((float)c2.X, (float)c2.Y)
						)
					));
				}
				else
				{
					var c0 = ToShapeSpace(currentOn);
					var c1 = ToShapeSpace(firstPt);

					shape.AddEdge(new EdgeSegment(
					new LinearSegment(
						new Vector2((float)c0.X, (float)c0.Y),
						new Vector2((float)c1.X, (float)c1.Y)
						)
					));

				}
				start = end + 1;
			}

			return shape;
		}


		private static Vector2 ToVec(GlyphPointF pt, float scale) =>
			new(pt.P.X / scale, pt.P.Y / scale);

		public static bool GetKerning(out float kerning, Typeface font, uint unicode1, uint unicode2, FontCoordinateScaling scaling)
		{
			kerning = 0;
			if (font.KernTable == null)
				return false;

			kerning = font.GetKernDistance(font.GetGlyphIndex((int)unicode1), font.GetGlyphIndex((int)unicode2));

			if (kerning == 0)
				return false;

			kerning *= GetFontCoordinateScale(font, scaling);
			return true;
		}

	}
}