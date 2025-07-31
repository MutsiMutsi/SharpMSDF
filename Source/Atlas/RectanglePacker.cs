using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace SharpMSDF.Atlas
{
    public class RectanglePacker
    {
        public const int WORST_FIT = 0x7fffffff;


        public RectanglePacker() { }
        public RectanglePacker(int width, int height) 
        {
            if (width > 0 && height > 0)
                _Spaces.Add(new Rectangle(0, 0, width, height));
        }


        static void RemoveFromUnorderedVector<T>(ref Span<T> span, int index)
        {
            if (index != span.Length - 1)
                span[index] = span[^1];
            span = span.Slice(0, span.Length - 1);
        }
        static void RemoveFromUnorderedVector<T>(List<T> list, int index)
        {
            if (index != list.Count - 1)
                list[index] = list[^1];
            list.RemoveAt(list.Count - 1);
        }

        /// Expands the packing area - both width and height must be greater or equal to the previous value
        public void Expand(int width, int height)
        {
            if (width > 0 && height > 0)
            {
                int oldWidth = 0, oldHeight = 0;
                for (int s = 0; s < _Spaces.Count; s++) {
                    var space = _Spaces[s];
                    if (space.X + space.Width > oldWidth)
                        oldWidth = space.X + space.Width;
                    if (space.Y + space.Height > oldHeight)
                        oldHeight = space.Y + space.Height;
                }
                _Spaces.Add(new Rectangle(0, 0, width, height ));
                SplitSpace(_Spaces.Count - 1, oldWidth, oldHeight);
            }
        }
        /// Packs the rectangle array, returns how many didn't fit (0 on success)
        public int Pack<TRect>(List<TRect> rectangles)
            where TRect : IRectangle
        {
            Span<int> remainingRects = stackalloc int[rectangles.Count];

            for (int i = 0; i < rectangles.Count; ++i)
                remainingRects[i] = i;
            while (remainingRects.Length > 0)
            {
                int bestFit = WORST_FIT;
                int bestSpace = -1;
                int bestRect = -1;
                TRect rect;
                for (int i = 0; i < _Spaces.Count; ++i)
                {
                    Rectangle space = _Spaces[i];
                    for (int j = 0; j < remainingRects.Length; ++j)
                    {
                        rect = rectangles[remainingRects[j]];
                        if (rect.Width == space.Width && rect.Height == space.Height)
                        {
                            bestSpace = i;
                            bestRect = j;
                            goto BEST_FIT_FOUND;
                        }
                        if (rect.Width <= space.Width && rect.Height <= space.Height)
                        {
                            int fit = RateFit(rect.Width, rect.Height, space.Width, space.Height);
                            if (fit < bestFit)
                            {
                                bestSpace = i;
                                bestRect = j;
                                bestFit = fit;
                            }
                        }
                    }
                }
                if (bestSpace < 0 || bestRect < 0)
                    break;

                BEST_FIT_FOUND:

                rect = rectangles[remainingRects[bestRect]];
                rect.X = _Spaces[bestSpace].X;
                rect.Y = _Spaces[bestSpace].Y;
                SplitSpace(bestSpace, rect.Width, rect.Height);
                RemoveFromUnorderedVector(ref remainingRects, bestRect);
            }
            return remainingRects.Length;
        }
        public int Pack(List<Rectangle> spaces, List<OrientedRectangle> rectangles)
        {
            Span<int> remainingRects = stackalloc int[rectangles.Count];

            for (int i = 0; i < rectangles.Count; ++i)
                remainingRects[i] = i;
            while (remainingRects.Length > 0)
            {
                int bestFit = WORST_FIT;
                int bestSpace = -1;
                int bestRect = -1;
                bool bestRotated = false;
                Rectangle rect;
                for (int i = 0; i < spaces.Count; ++i)
                {
                    Rectangle space = spaces[i];
                    for (int j = 0; j < remainingRects.Length; ++j)
                    {
                        rect = rectangles[remainingRects[j]];
                        if (rect.Width == space.Width && rect.Height == space.Height)
                        {
                            bestSpace = i;
                            bestRect = j;
                            bestRotated = false;
                            goto BEST_FIT_FOUND;
                        }
                        if (rect.Height == space.Width && rect.Width == space.Height)
                        {
                            bestSpace = i;
                            bestRect = j;
                            bestRotated = true;
                            goto BEST_FIT_FOUND;
                        }
                        if (rect.Width <= space.Width && rect.Height <= space.Height)
                        {
                            int fit = RateFit(rect.Width, rect.Height, space.Width, space.Height);
                            if (fit < bestFit)
                            {
                                bestSpace = i;
                                bestRect = j;
                                bestRotated = false;
                                bestFit = fit;
                            }
                        }
                        if (rect.Height <= space.Width && rect.Width <= space.Height)
                        {
                            int fit = RateFit(rect.Height, rect.Width, space.Width, space.Height);
                            if (fit < bestFit)
                            {
                                bestSpace = i;
                                bestRect = j;
                                bestRotated = true;
                                bestFit = fit;
                            }
                        }

                    }
                }
                if (bestSpace < 0 || bestRect < 0)
                    break;

                BEST_FIT_FOUND:

                rect = rectangles[remainingRects[bestRect]];
                rect.X = spaces[bestSpace].X;
                rect.Y = spaces[bestSpace].Y;
                if (bestRotated)
                    SplitSpace(bestSpace, rect.Height, rect.Width);
                else
                    SplitSpace(bestSpace, rect.Width, rect.Height);
                    
                RemoveFromUnorderedVector(ref remainingRects, bestRect);
            }
            return remainingRects.Length;
        }

        List<Rectangle> _Spaces = [];

        static int RateFit(int w, int h, int sw, int sh)
        {
            return Math.Min(sw-w, sh-h); // TODO: sus
        }

        void SplitSpace(int index, int w, int h)
        {
            Rectangle space = _Spaces[index];
            RemoveFromUnorderedVector(_Spaces, index);
            Rectangle a = new ( space.X, space.Y + h, w, space.Height - h );
            Rectangle b = new ( space.X + w, space.Y, space.Width - w, h );
            if (w * (space.Height - h) < h * (space.Width - w))
                a.Width = space.Width;
            else
                b.Height = space.Height;
            if (a.Width > 0 && a.Height > 0)
                _Spaces.Add(a);
            if (b.Width > 0 && b.Height > 0)
                _Spaces.Add(b);
        }

        public static void CopyRectanglePlacement(ref Rectangle dst, Rectangle src) {
            dst.X = src.X;
            dst.Y = src.Y;
        }

        public static void CopyRectanglePlacement(ref OrientedRectangle dst, OrientedRectangle src)
        {
            dst.X = src.X;
            dst.Y = src.Y;
            dst.Rotated = src.Rotated;
        }


    /// <summary>
    /// Packs the rectangle array into an atlas with fixed dimensions.
    /// Returns the error code (0 on success, >0 if some didn't fit).
    /// </summary>
    public static int Pack<T>(List<T> rectangles, int width, int height, int spacing = 0)
            where T : IRectangle
        {
            // Expand each box by spacing
            if (spacing != 0)
            {
                foreach (var r in rectangles)
                {
                    r.Width += spacing;
                    r.Height += spacing;
                }
            }

            // Delegate to your instance-based packer
            var packer = new RectanglePacker(width + spacing, height + spacing);
            int result = packer.Pack(rectangles);

            // Shrink back
            if (spacing != 0)
            {
                foreach (var r in rectangles)
                {
                    r.Width -= spacing;
                    r.Height -= spacing;
                }
            }

            return result;
        }

        /// <summary>
        /// Packs the rectangle array into an atlas of unknown size.
        /// Uses a size‐selector to iterate possible atlas sizes until it fits.
        /// Returns the chosen (width, height).
        /// </summary>
        public static (int Width, int Height) PackWithSelector<SizeSelector, TRect>(List<TRect> rectangles, int spacing = 0)
            where TRect : IRectangle, new()
            where SizeSelector : ISizeSelector, new()
        {
            // Make a copy and expand by spacing
            var copy = rectangles
                .Select(r =>
                {
                    var c = new TRect();
                    c.Width = r.Width + spacing;
                    c.Height = r.Height + spacing;
                    return c;
                })
                .ToArray();

            // Compute total area (without spacing)
            int totalArea = rectangles.Sum(r => r.Width * r.Height);

            // Initialize selector
            var selector = new SizeSelector();
            selector.Initialize(totalArea);

            (int Width, int Height) dimensions = default;

            while (selector.Next(out int w, out int h))
            {
                //TODO : WORK ON THIS
                var packer = new RectanglePacker(w + spacing, h + spacing);
                //if (packer.Pack(copy) == 0)
                {
                    // success: record dims and copy placements back
                    dimensions = (w, h);
                    for (int i = 0; i < rectangles.Count; i++)
                    {
                        //if (rectangles[i] is Rectangle rect)
                            //CopyRectanglePlacement(ref rect, copy[i]);

                    }
                    selector.Decrement();
                }
                //else
                {
                    selector.Increment();
                }
            }

            return dimensions;
        }
    }
}
