using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using SharpMSDF.Core;
using SharpMSDF.IO;
using Typography.OpenFont;

namespace SharpMSDF.Atlas
{
	/// <summary>
	/// Represents the geometry of all glyphs of a given font or font variant
	/// </summary
	public class FontGeometry
	{
		private float geometryScale = 1.0f;
		private FontMetrics metrics;
		private GlyphIdentifierType preferredIdentifierType = GlyphIdentifierType.UnicodeCodepoint;
		private ushort rangeStart, rangeEnd;
		private readonly Dictionary<(ushort, ushort), float> kerning = new();
		private string? name;

		/// <summary>
		/// Loads the consecutive range of glyphs between rangeStart (inclusive) and rangeEnd (exclusive), returns the number of successfully loaded glyphs
		/// </summary>
		/*public int LoadGlyphRange(Typeface font, float fontScale, ushort rangeStart, uint rangeEnd, bool preprocessGeometry = true, bool enableKerning = true)
		{
			if (!(glyphs.Count == this.rangeEnd && LoadMetrics(font, fontScale)))
				return -1;

			int loaded = 0;
			for (ushort index = rangeStart; index < rangeEnd; ++index)
			{
				var glyph = new GlyphGeometry();
				if (glyph.Load(font, geometryScale, index, preprocessGeometry))
				{
					AddGlyph(glyph);
					++loaded;
				}
			}

			if (enableKerning)
				LoadKerning(font);

			preferredIdentifierType = GlyphIdentifierType.GlyphIndex;
			return loaded;
		}*/

		/// <summary>
		/// Loads all glyphs in a glyphset (Charset elements are glyph indices), returns the number of successfully loaded glyphs
		/// </summary>
		/*public int LoadGlyphset(Typeface face, float fontScale, Charset glyphset, bool preprocessGeometry = true, bool enableKerning = true)
        {
            if (!(glyphs.Count == rangeEnd && LoadMetrics(face, fontScale)))
                return -1;

            int loaded = 0;
            foreach (uint index in glyphset)
            {
                var glyph = new GlyphGeometry();
                if (glyph.Load(face, geometryScale, index, preprocessGeometry))
                {
                    AddGlyph(glyph);
                    ++loaded;
                }
            }

            if (enableKerning)
                LoadKerning(face);

            preferredIdentifierType = GlyphIdentifierType.GlyphIndex;
            return loaded;
        }
        */

		/// <summary>
		/// Loads all glyphs in a charset (Charset elements are Unicode codepoints), returns the number of successfully loaded glyphs
		/// </summary>
		public int LoadCharset(List<Shape> shapes, Typeface face, float fontScale, ReadOnlySpan<char> charset, List<GlyphGeometry> glyphSpan, bool preprocessGeometry = true)
		{
			LoadMetrics(face, fontScale);

			int loaded = 0;

			for (int i = 0; i < charset.Length; i++)
			{
				var glyph = new GlyphGeometry();
				if (glyph.Load(shapes[i], face, geometryScale, charset[i], preprocessGeometry))
				{
					glyphSpan[loaded] = glyph;
					++loaded;
				}
			}

			preferredIdentifierType = GlyphIdentifierType.UnicodeCodepoint;
			return loaded;
		}

		///<summary>
		/// Only loads font metrics and geometry scale from font
		///</summary>
		public bool LoadMetrics(Typeface font, float fontScale)
		{
			if (!FontImporter.GetFontMetrics(out this.metrics, font, FontCoordinateScaling.None))
				return false;

			if (metrics.EmSize <= 0)
				metrics.EmSize = 2048.0f;

			geometryScale = fontScale / metrics.EmSize;

			metrics.EmSize *= geometryScale;
			metrics.AscenderY *= geometryScale;
			metrics.DescenderY *= geometryScale;
			metrics.LineHeight *= geometryScale;
			metrics.UnderlineY *= geometryScale;
			//metrics.UnderlineThickness *= geometryScale;

			return true;
		}

		/// <summary>
		/// Loads kerning pairs for all glyphs that are currently present, returns the number of loaded kerning pairs
		/// </summary>
		public int LoadKerning(IEnumerable<char> glyphs, Typeface font)
		{
			int loaded = 0;

			for (int i = 0; i < glyphs.Count(); ++i)
			{
				for (int j = 0; j < glyphs.Count(); ++j)
				{
					var glyph1 = glyphs.ElementAt(i);
					var glyph2 = glyphs.ElementAt(j);

					if (FontImporter.GetKerning(out float advance, font, glyph1, glyph2, FontCoordinateScaling.None) && advance != 0.0)
					{
						kerning[(glyph1, glyph2)] = advance * geometryScale;
						++loaded;
					}
				}
			}

			return loaded;
		}

		/// <summary>
		/// Sets a name to be associated with the font
		/// </summary>
		public void SetName(string? name)
		{
			this.name = name;
		}

		public float GeometryScale => geometryScale;
		public FontMetrics Metrics => metrics;
		public GlyphIdentifierType PreferredIdentifierType => preferredIdentifierType;

		/// <summary>
		/// Finds a glyph by glyph index, returns null if not found
		/// </summary>
		/*public GlyphGeometry? GetGlyph(ushort index)
		{
			return glyphsByIndex.TryGetValue(index, out var i) ? glyphs[i] : null;
		}

		/// <summary>
		/// Finds a glyph by glyph Unicode codepoint, returns null if not found
		/// </summary>
		public GlyphGeometry? GetGlyph(uint codepoint)
		{
			return glyphsByCodepoint.TryGetValue(codepoint, out var i) ? glyphs[i] : null;
		}

		/// <summary>
		/// Outputs the advance between two glyphs with kerning taken into consideration, returns false on failure
		/// </summary>
		public bool GetAdvance(out float advance, ushort index1, ushort index2)
		{
			advance = 0;
			var glyph1 = GetGlyph(index1);
			if (!glyph1.HasValue)
				return false;

			advance = glyph1.Value.GetAdvance();
			if (kerning.TryGetValue((index1, index2), out float kern))
				advance += kern;

			return true;
		}

		/// <summary>
		/// Outputs the advance between two glyphs with kerning taken into consideration, returns false on failure
		/// </summary>
		public bool GetAdvance(out float advance, uint codepoint1, uint codepoint2)
		{
			advance = 0;
			var glyph1 = GetGlyph(codepoint1);
			var glyph2 = GetGlyph(codepoint2);
			if (!glyph1.HasValue || !glyph2.HasValue)
				return false;

			advance = glyph1.Value.GetAdvance();
			if (kerning.TryGetValue((glyph1.Value.GetIndex(), glyph2.Value.GetIndex()), out float kern))
				advance += kern;

			return true;
		}*/

		public IReadOnlyDictionary<(ushort, ushort), float> Kerning => kerning;
		public string? Name => string.IsNullOrEmpty(name) ? null : name;
	}
}
