
namespace SharpMSDF.Core
{
    public class DistanceMapping
    {
        // Explicitly designates value as distance delta rather than an absolute distance.
        public class Delta
        {
            public double Value { get; }

            public Delta(double distanceDelta)
            {
                Value = distanceDelta;
            }

            public static implicit operator double(Delta d) => d.Value;
        }

        private readonly double scale;
        private readonly double translate;

        public DistanceMapping()
        {
            scale = 1.0;
            translate = 0.0;
        }

        public DistanceMapping(DoubleRange range)
        {
            double extent = range.Upper - range.Lower;
            scale = extent != 0.0 ? 2.0 / extent : 1.0;
            translate = -scale * (range.Lower + range.Upper) * 0.5;
        }

        private DistanceMapping(double scale, double translate)
        {
            this.scale = scale;
            this.translate = translate;
        }

        public double this[double d] => d * scale + translate;

        public double this[Delta d] => d.Value * scale;

        public DistanceMapping Inverse()
        {
            return new DistanceMapping(scale != 0.0 ? 1.0 / scale : 1.0, -translate / scale);
        }

        public static DistanceMapping Inverse(DoubleRange range)
        {
            return new DistanceMapping(range).Inverse();
        }
    }

}