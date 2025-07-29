
using SharpMSDF.Atlas;

namespace SharpMSDF.Atlas
{
    public class ImmediateAtlasGenerator<T>
    {
        public readonly int N;
        public GeneratorFunction<T> GEN_FN { get; }
        public AtlasStorage Storage => storage;
        public List<GlyphBox> Layout => layout;

        private AtlasStorage storage;
        private readonly List<GlyphBox> layout = new();
        private List<T> glyphBuffer = new();
        private List<byte> errorCorrectionBuffer = new();
        private GeneratorAttributes attributes;
        private int threadCount = 1;

        public ImmediateAtlasGenerator(int n, GeneratorFunction<T> genFn, Func<int, int, AtlasStorage> storageCtor)
        {
            N = n;
            GEN_FN = genFn;
            storage = storageCtor(0, 0);
        }

        public ImmediateAtlasGenerator(int width, int height, int n, GeneratorFunction<T> genFn, Func<int, int, AtlasStorage> storageCtor)
        {
            N = n;
            GEN_FN = genFn;
            storage = storageCtor(width, height);
        }

        public void Generate(GlyphGeometry[] glyphs)
        {
            int maxBoxArea = 0;
            for (int i = 0; i < glyphs.Length; ++i)
            {
                GlyphBox box = glyphs[i];
                maxBoxArea = Math.Max(maxBoxArea, box.rect.w * box.rect.h);
                layout.Add(box);
            }

            int threadBufferSize = N * maxBoxArea;
            if (threadCount * threadBufferSize > glyphBuffer.Count)
                glyphBuffer = new List<T>(new T[threadCount * threadBufferSize]);
            if (threadCount * maxBoxArea > errorCorrectionBuffer.Count)
                errorCorrectionBuffer = new List<byte>(new byte[threadCount * maxBoxArea]);

            var threadAttributes = new GeneratorAttributes[threadCount];
            for (int i = 0; i < threadCount; ++i)
            {
                threadAttributes[i] = attributes;
                threadAttributes[i].Config.ErrorCorrection.Buffer = errorCorrectionBuffer.GetRange(i * maxBoxArea, maxBoxArea).ToArray();
            }

            var workload = new Workload((i, threadNo) =>
            {
                var glyph = glyphs[i];
                if (!glyph.IsWhitespace())
                {
                    glyph.GetBoxRect(out int l, out int b, out int w, out int h);
                    var span = glyphBuffer.GetRange(threadNo * threadBufferSize, threadBufferSize).ToArray();
                    var glyphBitmap = new BitmapRef<T>(span, w, h, N);
                    GEN_FN(glyphBitmap, glyph, threadAttributes[threadNo]);
                    var constRef = new BitmapConstRef<T>(glyphBitmap);
                    ((dynamic)storage).Put(l, b, constRef);
                }
                return true;
            }, glyphs.Length);

            workload.Finish(threadCount);
        }

        public void Rearrange(int width, int height, Remap[] remapping)
        {
            for (int i = 0; i < remapping.Length; ++i)
            {
                layout[remapping[i].Index].rect.x = remapping[i].target.x;
                layout[remapping[i].index].rect.y = remapping[i].target.y;
            }

            var newStorage = (AtlasStorage)Activator.CreateInstance(typeof(AtlasStorage),
                args: new object[] { storage, width, height, remapping });
            storage = newStorage;
        }

        public void Resize(int width, int height)
        {
            var newStorage = (AtlasStorage)Activator.CreateInstance(typeof(AtlasStorage),
                args: new object[] { storage, width, height });
            storage = newStorage;
        }

        public void SetAttributes(GeneratorAttributes attributes)
        {
            this.attributes = attributes;
        }

        public void SetThreadCount(int threadCount)
        {
            this.threadCount = threadCount;
        }
    }

}