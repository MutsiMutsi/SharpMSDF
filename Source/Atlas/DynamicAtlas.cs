using System.Diagnostics;

namespace SharpMSDF.Atlas
{

    [Flags]
    public enum ChangeFlag : int
    {
        NoChange = 0x00,
        Resized = 0x01,
        Rearranged = 0x02,
    }

    /// <summary>
    /// This class can be used to produce a dynamic atlas to which more glyphs are added over time.
    /// It takes care of laying out and enlarging the atlas as necessary and delegates the actual work
    /// to the specified AtlasGenerator, which may e.g. do the work asynchronously.
    /// </summary>
    public class DynamicAtlas<TAtlasGen>
        where TAtlasGen : AtlasGenerator
    {
        private int _Side = 128;
        private int _Spacing;
        private int _GlyphCount;
        private int _TotalArea;
        private List<AtlasRectangle> _Rectangles = [];
        private List<Remap> _RemapBuffer = [];
        public RectanglePacker Packer;
        public TAtlasGen Generator;


        public DynamicAtlas() { }
        public DynamicAtlas(TAtlasGen generator, RectanglePacker packer, int minSide, int maxSide)
        {
            _Side = CeilPOT(minSide);
            _Spacing = 0;
            _GlyphCount = 0;
            _TotalArea = 0;
            Packer = packer;
            Generator = generator;
        }

        private static int CeilPOT(int x)
        {
            if (x > 0)
            {
                int y = 1;
                while (y < x)
                    y <<= 1;
                return y;
            }
            return 0;
        }

		public ChangeFlag Add(List<Shape> shapes, List<GlyphGeometry> glyphs, bool allowRearrange = false)
		{
			ChangeFlag changeFlags = 0;
			int start = _Rectangles.Count;
			int originalGlyphCount = _GlyphCount;

			var remapBuffer = new List<Remap>(glyphs.Count);

			for (int i = 0; i < glyphs.Count; ++i)
			{
				if (!glyphs[i].IsWhitespace)
				{
					glyphs[i].GetBoxSize(out int w, out int h);
					_Rectangles.Add(new AtlasRectangle(0, 0, w + _Spacing, h + _Spacing));
					remapBuffer.Add(new Remap
					{
						Index = originalGlyphCount + i,
						Width = w,
						Height = h
					});
					_TotalArea += (w + _Spacing) * (h + _Spacing);
				}
			}

			if (_Rectangles.Count > start)
			{
				int packerStart = start;
				int remaining;
				while ((remaining = Packer.Pack(_Rectangles, packerStart)) > 0)
				{
					_Side = (_Side | (_Side == 0 ? 1 : 0)) << 1;
					while (_Side * _Side < _TotalArea)
						_Side <<= 1;

					if (allowRearrange)
					{
						Packer = new RectanglePacker(_Side + _Spacing, _Side + _Spacing);
						packerStart = 0;
					}
					else
					{
						Packer.Expand(_Side + _Spacing, _Side + _Spacing);
						packerStart = _Rectangles.Count - remaining;
					}

					changeFlags |= ChangeFlag.Resized;
				}

				// Only need to call Rearrange/Resize if rearrangement/resizing occurred
				if (packerStart < start && allowRearrange)
				{
					for (int i = packerStart; i < start; ++i)
					{
						var remap = remapBuffer[i - start];
						remap.Source = remap.Target;
						remap.Target = new(_Rectangles[i].X, _Rectangles[i].Y);
						remapBuffer[i - start] = remap;
					}

					Generator.Rearrange(_Side, _Side, remapBuffer, start);
					changeFlags |= ChangeFlag.Rearranged;
				}
				else if ((changeFlags & ChangeFlag.Resized) != 0)
				{
					Generator.Resize(_Side, _Side);
				}

				for (int i = 0; i < remapBuffer.Count; ++i)
				{
					int rectIdx = start + i;
					glyphs[i] = glyphs[i].PlaceBox(_Rectangles[rectIdx].X, _Rectangles[rectIdx].Y);
				}

				Generator.Generate(shapes, glyphs); // only uses current batch
			}

			_GlyphCount += glyphs.Count;
			return changeFlags;
		}

	}
}
