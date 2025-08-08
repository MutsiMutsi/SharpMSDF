using SharpMSDF.Core;
using System.Buffers;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace SharpMSDF.Atlas
{
	public class ImmediateAtlasGenerator<T, TStorage> : AtlasGenerator
		where T : struct
		where TStorage : AtlasStorage
	{
		public readonly int N;
		public GeneratorFunction<float> GEN_FN { get; }
		public TStorage Storage { get; private set; }
		public List<GlyphBox> Layout { get; } = [];
		private GeneratorAttributes _Attributes = new GeneratorAttributes { Config = MSDFGeneratorConfig.Default };

		public ImmediateAtlasGenerator(int n, GeneratorFunction<float> genFn, TStorage storage)
		{
			N = n;
			GEN_FN = genFn;
			Storage = storage;
		}


		public override void Generate(List<Shape> shapes, List<GlyphGeometry> glyphs)
		{
			int maxBoxArea = 0;
			for (int i = 0; i < glyphs.Count; ++i)
			{
				GlyphBox box = glyphs[i];
				int boxArea = box.Rect.Width * box.Rect.Height;
				maxBoxArea = Math.Max(maxBoxArea, boxArea);
				Layout.Add(box);
			}

			// Pre-allocate buffer large enough for the biggest glyph
			int bufferSize = maxBoxArea * BitmapView.Channels;
			var arrayPool = ArrayPool<float>.Shared;
			var buffer = arrayPool.Rent(bufferSize);

			try
			{
				for (int i = 0; i < glyphs.Count; i++)
				{
					if (!glyphs[i].IsWhitespace)
					{
						glyphs[i].GetBoxRect(out int l, out int b, out int w, out int h);
						int requiredSize = w * h * BitmapView.Channels;

						// Buffer should always be large enough now
						Debug.Assert(buffer.Length >= requiredSize, "Pre-allocated buffer too small");

						Span<float> pixelSpan = buffer.AsSpan(0, requiredSize);
						var glyphBitmapView = new BitmapView(pixelSpan, w, h, 0, 0, w, h);
						GEN_FN(shapes[i], glyphBitmapView, glyphs[i], _Attributes);
						Storage.Put(l, b, glyphBitmapView);
					}
				}
			}
			finally
			{
				arrayPool.Return(buffer);
			}
		}

		public override void Rearrange(int width, int height, List<Remap> remapping, int count)
		{
			throw new NotImplementedException();
			/*for (int i = 0; i < count; ++i)
			{
				var glyphBox = Layout[remapping[i].Index] with { Rect = Layout[remapping[i].Index].Rect with { X = remapping[i].Target.X, Y = remapping[i].Target.Y } };

				Layout[remapping[i].Index] = glyphBox;
			}

			var oldStorage = Storage;
			Storage.Init(oldStorage, width, height, remapping.ToArray()[..count]);*/
		}

		public override void Resize(int width, int height)
		{
			Storage.Resize(width, height);
		}

		public void SetAttributes(GeneratorAttributes attributes)
		{
			_Attributes = attributes;
		}

	}
}