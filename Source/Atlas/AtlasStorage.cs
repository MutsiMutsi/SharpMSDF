using SharpMSDF.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharpMSDF.Atlas
{
    public abstract class AtlasStorage
    {

        public AtlasStorage() { }

        public abstract void Init(int width, int height, int channels);

        /// Creates a copy with different dimensions
        public abstract void Init<TStorage>(TStorage orig, int width, int height) 
            where TStorage : AtlasStorage;
        /// Creates a copy with different dimensions and rearranges the pixels according to the remapping array
        public abstract void Init<TStorage>(TStorage orig, int width, int height, Span<Remap> remapping)
            where TStorage : AtlasStorage;
        /// Stores a subsection at x, y into the atlas _Storage. May be implemented for only some TRect, N
        public abstract void Put<T>(int x, int y, BitmapConstRef<T> subBitmap) where T : struct;
        /// Retrieves a subsection at x, y from the atlas _Storage. May be implemented for only some TRect, N
        public abstract void Get<T>(int x, int y, BitmapRef<T> subBitmap) where T : struct;

    };

}

