namespace SharpMSDF.Atlas
{
	public abstract class AtlasStorage
	{
		public abstract int Size { get; }
		public AtlasStorage() { }
		public abstract void Init(int width, int height);
		public abstract bool Resize(int width, int height);
		/// Creates a copy with different dimensions and rearranges the pixels according to the remapping array
		/// Stores a subsection at x, y into the atlas _Storage. May be implemented for only some TRect, N
		public abstract void Put(int x, int y, BitmapView subBitmap);
		/// Retrieves a subsection at x, y from the atlas _Storage. May be implemented for only some TRect, N
		public abstract void Get(int x, int y, BitmapView subBitmap);
	};
}

