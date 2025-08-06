using SkiaSharp;

namespace SharpMSDF.Core
{
	public struct BitmapRef
	{
		/// <summary>
		/// Bitmap array that we reference from
		/// </summary>
		public readonly float[] Pixels;
		public readonly int OriginalWidth, OriginalHeight;
		public readonly int SubX, SubY, SubWidth, SubHeight;
		/// <summary>
		/// Number of channels
		/// </summary>
		public readonly int N;

		public BitmapRef(Bitmap bitmap, int subX = 0, int subY = 0, int subW = -1, int subH = -1)
			: this(bitmap.Pixels, bitmap.Width, bitmap.Height, bitmap.N, subX, subY, subW, subH) { }

		public BitmapRef(float[] pixels, int width, int height, int n = 1, int subX = 0, int subY = 0, int subW = -1, int subH = -1)
		{
			OriginalWidth = width;
			OriginalHeight = height;
			SubX = subX;
			SubY = subY;
			SubWidth = (subW < 0) ? width : subW;
			SubHeight = (subH < 0) ? height : subH;
			Pixels = pixels;
			N = n;
		}

		public readonly Span<float> Slice(int x, int y) => Pixels.AsSpan(GetIndex(x, y), N);
		public readonly int GetIndex(int x, int y, int channel = 0) => N * (OriginalWidth * (SubY + y) + SubX + x) + channel;
		public ref float this[int x, int y, int channel = 0] => ref Pixels[GetIndex(x, y, channel)];

		public static implicit operator BitmapRef(Bitmap bitmapRef) => new BitmapRef(bitmapRef);
	}

	/// <summary>
	/// Constant reference to a 2D image bitmap or a buffer acting as one. Pixel storage not owned or managed by the object.
	/// </summary>
	public readonly struct BitmapConstRef
	{
		internal readonly float[] _Pixels;
		public readonly int OriginalWidth, OriginalHeight;
		public readonly int SubX, SubY, SubWidth, SubHeight;
		public readonly int N;

		public BitmapConstRef(BitmapRef bitmapRef)
			: this(bitmapRef.Pixels, bitmapRef.OriginalWidth, bitmapRef.OriginalHeight, bitmapRef.N, bitmapRef.SubX, bitmapRef.SubY, bitmapRef.SubWidth, bitmapRef.SubHeight) { }

		public BitmapConstRef(Bitmap bitmap, int subX = 0, int subY = 0, int subW = -1, int subH = -1)
			: this(bitmap.Pixels, bitmap.Width, bitmap.Height, bitmap.N, subX, subY, subW, subH) { }

		public BitmapConstRef(float[] pixels, int width, int height, int n = 1, int subX = 0, int subY = 0, int subW = -1, int subH = -1)
		{
			OriginalWidth = width;
			OriginalHeight = height;
			SubX = subX;
			SubY = subY;
			SubWidth = (subW < 0) ? width : subW;
			SubHeight = (subH < 0) ? height : subH;
			_Pixels = pixels;
			N = n;
		}

		public readonly ReadOnlySpan<float> Slice(int x, int y) => _Pixels.AsSpan(GetIndex(x, y), N);
		public readonly int GetIndex(int x, int y, int channel = 0) => N * (OriginalWidth * (SubY + y) + SubX + x) + channel;
		public float this[int x, int y, int channel = 0] => _Pixels[GetIndex(x, y, channel)];

		public static implicit operator BitmapConstRef(BitmapRef bitmapRef) => new BitmapConstRef(bitmapRef);
		public static implicit operator BitmapConstRef(Bitmap bitmap) => new BitmapConstRef(bitmap);
	}
}
