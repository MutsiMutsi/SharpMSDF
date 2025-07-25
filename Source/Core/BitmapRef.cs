using SharpMSDF.Core;

namespace SharpMSDF.Core
{
    public unsafe struct BitmapRef<T>
    {
        public T* Pixels;
        public int Width, Height;
        public int N;

        public BitmapRef(T* pixels, int width, int height, int n = 1)
        {
            Pixels = pixels;
            Width = width;
            Height = height;
            N = n;
        }
        public BitmapRef(T[] pixels, int width, int height, int n = 1)
        {
            fixed (T* ptr = pixels)
                Pixels = ptr;
            
            Width = width;
            Height = height;
            N = n;
        }

        public ref T this[int x, int y]
        {
            get
            {
                int index = N * (Width * y + x);
                return ref Pixels[index];
            }
        }
    }

}


/// Constant reference to a 2D image bitmap or a buffer acting as one. Pixel storage not owned or managed by the object.
public unsafe readonly struct BitmapConstRef<T>
{
    public readonly T* Pixels;
    public readonly int Width, Height;
    public readonly int N;

    public BitmapConstRef(T* pixels, int width, int height, int n = 1)
    {
        Pixels = pixels;
        Width = width;
        Height = height;
        N = n;
    }
    public BitmapConstRef(T[] pixels, int width, int height, int n = 1)
    {
        fixed (T* ptr = pixels)
        {
            Pixels = ptr;
        }
        Width = width;
        Height = height;
        N = n;
    }

    public BitmapConstRef(BitmapRef<T> original)
    {
        Pixels = original.Pixels;
        Width = original.Width;
        Height = original.Height;
        N = original.N;
    }

    public T this[int x, int y]
    {
        get
        {
            int index = N * (Width * y + x);
            return Pixels[index];
        }
    }
}
