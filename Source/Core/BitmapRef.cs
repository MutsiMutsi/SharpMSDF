public readonly ref struct BitmapView
{
	private readonly Span<float> _pixels;   // full pixel buffer (must be pinned or in scope)
	private readonly int _fullWidth;        // full image width
	private readonly int _subX, _subY;      // sub-rectangle offset
	public readonly int Width, Height;      // sub-rectangle size
	public const int Channels = 3;

	public int SubWidth => Width;
	public int SubHeight => Height;

	public BitmapView(Span<float> pixels, int fullWidth, int fullHeight,
					  int subX, int subY, int subWidth, int subHeight)
	{
		_pixels = pixels;
		_fullWidth = fullWidth;
		_subX = subX;
		_subY = subY;
		Width = subWidth;
		Height = subHeight;
	}

	// Index into pixel buffer (read/write)
	public ref float Get(int x, int y, int channel = 0)
	{
		if (x < 0 || x >= Width || y < 0 || y >= Height)
			throw new ArgumentOutOfRangeException();

		int index = Channels * ((_subY + y) * _fullWidth + (_subX + x)) + channel;
		return ref _pixels[index];
	}

	internal Span<float> GetPixelSpan() => _pixels;

	public readonly int GetIndex(int x, int y, int channel = 0)
	{
		return channel + Channels * (x + Width * y);
	}

	// Optional indexer syntax
	public ref float this[int x, int y, int channel = 0] => ref Get(x, y, channel);
}