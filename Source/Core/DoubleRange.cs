
namespace SharpMSDF.Core
{
    /// <summary>
    /// Represents the range between two real values.
    /// For example, the range of representable signed distances.
    /// </summary>
    public struct DoubleRange
    {
        public double Lower, Upper;

        public DoubleRange(double symmetricalWidth = 0)
        {
            Lower = -0.5 * symmetricalWidth;
            Upper = 0.5 * symmetricalWidth;
        }

        public DoubleRange(double lowerBound, double upperBound)
        {
            Lower = lowerBound;
            Upper = upperBound;
        }

        public static DoubleRange operator *(DoubleRange range, double factor)
        {
            return new DoubleRange(range.Lower * factor, range.Upper * factor);
        }

        public static DoubleRange operator *(double factor, DoubleRange range)
        {
            return new DoubleRange(factor * range.Lower, factor * range.Upper);
        }

        public static DoubleRange operator /(DoubleRange range, double divisor)
        {
            return new DoubleRange(range.Lower / divisor, range.Upper / divisor);
        }

        public void MultiplyInPlace(double factor)
        {
            Lower *= factor;
            Upper *= factor;
        }

        public void DivideInPlace(double divisor)
        {
            Lower /= divisor;
            Upper /= divisor;
        }

        public DoubleRange Multiply(double factor)
        {
            return new DoubleRange(Lower * factor, Upper * factor);
        }

        public DoubleRange Divide(double divisor)
        {
            return new DoubleRange(Lower / divisor, Upper / divisor);
        }

        public static DoubleRange operator +(DoubleRange a, DoubleRange b) => new DoubleRange(a.Lower + b.Lower, a.Upper + b.Upper);

    }
}
