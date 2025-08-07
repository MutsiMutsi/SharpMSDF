using SharpMSDF.Core;
using System.Buffers;
using System.Collections.Concurrent;
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
		private int _ThreadCount = 1;

		public ImmediateAtlasGenerator(int n, GeneratorFunction<float> genFn, TStorage storage)
		{
			N = n;
			GEN_FN = genFn;
			Storage = storage;
		}

		public override void Generate(Span<GlyphGeometry> glyphs)
		{
			int maxBoxArea = 0;

			for (int i = 0; i < glyphs.Length; ++i)
			{
				GlyphBox box = glyphs[i];
				maxBoxArea = Math.Max(maxBoxArea, box.Rect.Width * box.Rect.Height);
				Layout.Add(box);
			}

			int threadBufferSize = N * maxBoxArea;
			GeneratorAttributes[] threadAttributes = new GeneratorAttributes[_ThreadCount];

			for (int i = 0; i < _ThreadCount; ++i)
			{
				threadAttributes[i] = _Attributes;
			}

			var arrayPool = ArrayPool<float>.Shared;

			// Use ConcurrentBag to track rented arrays for proper cleanup
			var rentedArrays = new ConcurrentBag<float[]>();
			var threadBuffers = new ThreadLocal<float[]>(() =>
			{
				var buffer = arrayPool.Rent(threadBufferSize);
				rentedArrays.Add(buffer);
				return buffer;
			});

			/*try
			{
				Parallel.For(0, glyphs.Length, new ParallelOptions { MaxDegreeOfParallelism = _ThreadCount },
					i =>
			{*/
			for (int i = 0; i < glyphs.Length; i++)
			{
				GlyphGeometry glyph = glyphs[i];
				if (!glyph.IsWhitespace())
				{
					glyph.GetBoxRect(out int l, out int b, out int w, out int h);
					var buffer = threadBuffers.Value!;
					int requiredSize = w * h * BitmapView.Channels;

					if (buffer.Length < requiredSize)
					{
						// Return old buffer and rent a new larger one
						arrayPool.Return(buffer);
						buffer = arrayPool.Rent(requiredSize);
						rentedArrays.Add(buffer);
						threadBuffers.Value = buffer;
					}

					Span<float> pixelSpan = buffer.AsSpan(0, requiredSize);
					BitmapView glyphBitmapView = new BitmapView(pixelSpan, w, h, 0, 0, w, h);

					int threadNo = Thread.CurrentThread.ManagedThreadId % _ThreadCount;

					GEN_FN(glyphBitmapView, glyph, threadAttributes[threadNo]);
					Storage.Put(l, b, glyphBitmapView);
				}
			}
			//);
			/*}
			finally
			{
				// Return all rented arrays
				foreach (var buffer in rentedArrays)
				{
					arrayPool.Return(buffer);
				}

				threadBuffers.Dispose();
			}*/
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

		public void SetThreadCount(int threadCount)
		{
			_ThreadCount = threadCount;
		}
	}
}