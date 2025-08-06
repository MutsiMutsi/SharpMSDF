using SharpMSDF.Core;

public class Bitmap
{
	public readonly float[] Pixels;
	public readonly int Width;
	public readonly int Height;
	public const int Channels = 3;

	public Bitmap(int width, int height)
	{
		Width = width;
		Height = height;
		Pixels = new float[width * height * Channels];
	}

	public ref float GetPixel(int x, int y, int channel = 0)
	{
		int idx = Channels * (y * Width + x) + channel;
		return ref Pixels[idx];
	}

	// Returns a BitmapView for the whole image
	public BitmapView AsView() => new BitmapView(Pixels.AsSpan(), Width, Height, 0, 0, Width, Height);
}