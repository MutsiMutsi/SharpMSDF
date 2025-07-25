using System;
using System.Runtime.CompilerServices;

namespace SharpMSDF.Core
{
    public static class Arithmetic
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float Clamp(float n)
        {
            return n >= 0.0f && n <= 1.0f ? n : (n > 0f)? 1.0f: 0.0f;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double Clamp(double n)
        {
            return n >= 0.0 && n <= 1.0 ? n : (n > 0)? 1.0: 0.0;
        }

        /// Returns the middle out of three values
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float Median(float a, float b, float c)
        {
            return Math.Max(Math.Min(a, b), Math.Min(Math.Max(a, b), c));
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double Median(double a, double b, double c)
        {
            return Math.Max(Math.Min(a, b), Math.Min(Math.Max(a, b), c));
        }

        /// Returns the weighted average of a and b.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2 Mix(Vector2 a, Vector2 b, double weight)
        {
            return (1.0 - weight) * a + weight * b;
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float Mix(float a, float b, double weight)
        {
            return (float) ((1.0 - weight) * a + weight * b);
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float Mix(double a, double b, double weight)
        {
            return (float) ((1.0 - weight) * a + weight * b);
        }

        /// Returns 1 for positive values, -1 for negative values, and 0 for zero.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int Sign(double n)
        {
            return (0 < n ? 1 : 0) - (n < 0 ? 1 : 0);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int NonZeroSign(double n)
        {
            return n > 0 ? 1 : -1;
        }
    }
}