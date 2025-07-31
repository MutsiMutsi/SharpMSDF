namespace SharpMSDF.Atlas
{
    public interface IRectangle
    {
        int X { get; set; }
        int Y { get; set; }
        int Width { get; set; }
        int Height { get; set; }
    }

    public struct Rectangle : IRectangle
    {
        public int X { get; set; }
        public int Y { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }

        public Rectangle(int x, int y, int w, int h)
        {
            X = x;
            Y = y;
            Width = w;
            Height = h;
        }
    }

    public struct OrientedRectangle : IRectangle
    {
        public int X { get; set; }
        public int Y { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
        public bool Rotated;

        public static implicit operator Rectangle(OrientedRectangle rect) => new Rectangle(rect.X, rect.Y, rect.Width, rect.Height);
    };
}
