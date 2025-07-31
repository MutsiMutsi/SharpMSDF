using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using SharpMSDF.Core;
using SharpMSDF.IO;
using Typography.OpenFont;

namespace SharpMSDF.Atlas
{

    //TODO: sus 
    public class FontGeometry
    {
        public class GlyphRange
        {
            private readonly List<GlyphGeometry> glyphs;
            private readonly int rangeStart, rangeEnd;

            public GlyphRange()
            {
                glyphs = null!;
                rangeStart = rangeEnd = 0;
            }

            public GlyphRange(List<GlyphGeometry> glyphs, int rangeStart, int rangeEnd)
            {
                this.glyphs = glyphs;
                this.rangeStart = rangeStart;
                this.rangeEnd = rangeEnd;
            }

            public int Size => rangeEnd - rangeStart;
            public bool Empty => Size == 0;

            public List<GlyphGeometry>.Enumerator GetEnumerator()
            {
                return glyphs.GetRange(rangeStart, rangeEnd - rangeStart).GetEnumerator();
            }

            public ReadOnlySpan<GlyphGeometry> AsSpan()
            {
                return CollectionsMarshal.AsSpan(glyphs).Slice(rangeStart, rangeEnd - rangeStart);
            }
        }

        private double geometryScale = 1.0;
        private FontMetrics metrics;
        private GlyphIdentifierType preferredIdentifierType = GlyphIdentifierType.UnicodeCodepoint;
        private List<GlyphGeometry> glyphs;
        private ushort rangeStart, rangeEnd;
        private readonly Dictionary<ushort, ushort> glyphsByIndex = new();
        private readonly Dictionary<uint, ushort> glyphsByCodepoint = new();
        private readonly Dictionary<(ushort, ushort), double> kerning = new();
        private readonly List<GlyphGeometry> ownGlyphs = new();
        private string? name;

        public FontGeometry()
        {
            glyphs = ownGlyphs;
            rangeStart = rangeEnd = (ushort)glyphs.Count;
        }

        public FontGeometry(List<GlyphGeometry> externalGlyphStorage)
        {
            glyphs = externalGlyphStorage;
            rangeStart = rangeEnd = (ushort)glyphs.Count;
        }

        public int LoadGlyphRange(Typeface font, double fontScale, ushort rangeStart, uint rangeEnd, bool preprocessGeometry = true, bool enableKerning = true)
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
        }

        public int LoadGlyphset(Typeface face, double fontScale, Charset glyphset, bool preprocessGeometry = true, bool enableKerning = true)
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

        public int LoadCharset(Typeface face, double fontScale, Charset charset, bool preprocessGeometry = true, bool enableKerning = true)
        {
            if (!(glyphs.Count == rangeEnd && LoadMetrics(face, fontScale)))
                return -1;

            int loaded = 0;
            foreach (uint cp in charset)
            {
                var glyph = new GlyphGeometry();
                if (glyph.Load(face, geometryScale, cp, preprocessGeometry))
                {
                    AddGlyph(glyph);
                    ++loaded;
                }
            }

            if (enableKerning)
                LoadKerning(face);

            preferredIdentifierType = GlyphIdentifierType.UnicodeCodepoint;
            return loaded;
        }

        public bool LoadMetrics(Typeface font, double fontScale)
        {
            if (!FontImporter.GetFontMetrics(out metrics, font, FontCoordinateScaling.None))
                return false;

            if (metrics.EmSize <= 0)
                metrics.EmSize = 2048.0;

            geometryScale = fontScale / metrics.EmSize;

            metrics.EmSize *= geometryScale;
            metrics.AscenderY *= geometryScale;
            metrics.DescenderY *= geometryScale;
            metrics.LineHeight *= geometryScale;
            metrics.UnderlineY *= geometryScale;
            //metrics.UnderlineThickness *= geometryScale;

            return true;
        }

        public bool AddGlyph(GlyphGeometry glyph)
        {
            if (glyphs.Count != rangeEnd)
                return false;

            glyphsByIndex[glyph.GetIndex()] = rangeEnd;
            if (glyph.GetCodepoint() != 0)
                glyphsByCodepoint[glyph.GetCodepoint()] = rangeEnd;

            glyphs.Add(glyph);
            ++rangeEnd;
            return true;
        }

        public int LoadKerning(Typeface font)
        {
            int loaded = 0;

            for (int i = rangeStart; i < rangeEnd; ++i)
            {
                for (int j = rangeStart; j < rangeEnd; ++j)
                {
                    var glyph1 = glyphs[i];
                    var glyph2 = glyphs[j];

                    if (FontImporter.GetKerning(out double advance, font, glyph1.GetCodepoint(), glyph2.GetCodepoint(), FontCoordinateScaling.None) && advance != 0.0)
                    {
                        kerning[(glyph1.GetIndex(), glyph2.GetIndex())] = advance * geometryScale;
                        ++loaded;
                    }
                }
            }

            return loaded;
        }

        public void SetName(string? name)
        {
            this.name = name;
        }

        public double GeometryScale => geometryScale;
        public FontMetrics Metrics => metrics;
        public GlyphIdentifierType PreferredIdentifierType => preferredIdentifierType;
        public GlyphRange GetGlyphs() => new(glyphs, rangeStart, rangeEnd);

        public GlyphGeometry? GetGlyph(ushort index)
        {
            return glyphsByIndex.TryGetValue(index, out var i) ? glyphs[i] : null;
        }

        public GlyphGeometry? GetGlyph(uint codepoint)
        {
            return glyphsByCodepoint.TryGetValue(codepoint, out var i) ? glyphs[i] : null;
        }

        public bool GetAdvance(out double advance, ushort index1, ushort index2)
        {
            advance = 0;
            var glyph1 = GetGlyph(index1);
            if (!glyph1.HasValue)
                return false;

            advance = glyph1.Value.GetAdvance();
            if (kerning.TryGetValue((index1, index2), out double kern))
                advance += kern;

            return true;
        }

        public bool GetAdvance(out double advance, uint codepoint1, uint codepoint2)
        {
            advance = 0;
            var glyph1 = GetGlyph(codepoint1);
            var glyph2 = GetGlyph(codepoint2);
            if (!glyph1.HasValue || !glyph2.HasValue)
                return false;

            advance = glyph1.Value.GetAdvance();
            if (kerning.TryGetValue((glyph1.Value.GetIndex(), glyph2.Value.GetIndex()), out double kern))
                advance += kern;

            return true;
        }

        public IReadOnlyDictionary<(ushort, ushort), double> Kerning => kerning;
        public string? Name => string.IsNullOrEmpty(name) ? null : name;
    }
}
