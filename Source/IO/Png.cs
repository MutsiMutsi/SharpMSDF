using SharpMSDF.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharpMSDF.IO
{
    public class Png
    {
        public static bool SavePng(BitmapConstRef<float> bitmap, string filename)
        {
            Span<byte> pixels = new byte[4 * bitmap.SubWidth * bitmap.SubHeight];
            int idx = 0;
            for (int y = bitmap.SubHeight - 1; y >= 0; y--)
            {
                for (int x = 0; x < bitmap.SubWidth; x++)
                {
                    switch (bitmap.N)
                    {
                        case 1:
                            pixels[idx++] = Bmp.PixelFloatToByte(bitmap[x, y]);
                            pixels[idx++] = Bmp.PixelFloatToByte(bitmap[x, y]);
                            pixels[idx++] = Bmp.PixelFloatToByte(bitmap[x, y]);
                            pixels[idx++] = Bmp.PixelFloatToByte(255);
                            break;
                        case 3:
                            pixels[idx++] = Bmp.PixelFloatToByte(bitmap[x, y, 0]);
                            pixels[idx++] = Bmp.PixelFloatToByte(bitmap[x, y, 1]);
                            pixels[idx++] = Bmp.PixelFloatToByte(bitmap[x, y, 2]);
                            pixels[idx++] = Bmp.PixelFloatToByte(255);
                            break;
                        case 4:
                            pixels[idx++] = Bmp.PixelFloatToByte(bitmap[x, y, 0]);
                            pixels[idx++] = Bmp.PixelFloatToByte(bitmap[x, y, 1]);
                            pixels[idx++] = Bmp.PixelFloatToByte(bitmap[x, y, 2]);
                            pixels[idx++] = Bmp.PixelFloatToByte(bitmap[x, y, 3]);
                            break;
                    }
                }
            }
            try
            {
                using (var file = File.OpenWrite(filename))
                    MinimalPngEncoder.EncodeToStream(pixels, bitmap.SubWidth, bitmap.SubHeight, file);
            }
            catch
            {
                return false;
            }
            return true;
        }
    }
}
