using System;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace SharpMSDF.Core
{
	public static class VectorExtensions
	{
		public static Vector2 Normalize(this Vector2 v, bool allowZero = false)
		{
			float len = v.Length();
			if (len != 0)
				return new(v.X / len, v.Y / len);
			return new(0, allowZero ? 0.0f : 1.0f);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Vector2 GetOrthonormal(this Vector2 v, bool polarity = true, bool allowZero = false)
		{
			float len = v.Length();
			if (len != 0)
				return polarity ? new(-v.Y / len, v.X / len) : new(v.Y / len, -v.X / len);
			return polarity ? new(0, allowZero ? 0 : 1) : new(0, -(allowZero ? 0 : 1));
		}

		/// Returns a vector with the same length that is orthogonal to this one.
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Vector2 GetOrthogonal(this Vector2 v, bool polarity = true)
		{
			return polarity ? new(-v.Y, v.X) : new(v.Y, -v.X);
		}

		/// A special version of the cross product for 2D vectors (returns scalar value).
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static float Cross(Vector2 a, Vector2 b)
		{
			return a.X * b.Y - a.Y * b.X;
		}

		public static void NormalizeBatch(ReadOnlySpan<Vector2> input, Span<Vector2> output)
		{
			int count = input.Length;
			int simdWidth = Vector<float>.Count;

			// Process SIMD-width batches (2x Vector2 == simdWidth floats)
			int i = 0;
			for (; i + simdWidth / 2 <= count; i += simdWidth / 2)
			{
				// Load interleaved Vector2s into SoA for SIMD
				Span<float> x = stackalloc float[simdWidth];
				Span<float> y = stackalloc float[simdWidth];
				for (int j = 0; j < simdWidth / 2; j++)
				{
					x[j] = input[i + j].X;
					y[j] = input[i + j].Y;
				}

				var xVec = new Vector<float>(x);
				var yVec = new Vector<float>(y);

				var lenSq = xVec * xVec + yVec * yVec;
				var epsilon = new Vector<float>(1e-6f);
				var mask = Vector.GreaterThan(lenSq, epsilon);
				var invLen = new Vector<float>(1.0f) / Vector.SquareRoot(lenSq);

				var nx = xVec * invLen;
				var ny = yVec * invLen;

				nx = Vector.ConditionalSelect(mask, nx, Vector<float>.Zero);
				ny = Vector.ConditionalSelect(mask, ny, Vector<float>.Zero);

				// Store result back into Vector2 output
				nx.CopyTo(x);
				ny.CopyTo(y);
				for (int j = 0; j < simdWidth / 2; j++)
					output[i + j] = new Vector2(x[j], y[j]);
			}

			// Fallback for any remaining Vector2s
			for (; i < count; i++)
			{
				var v = input[i];
				float lenSq = v.LengthSquared();
				if (lenSq > 1e-6f)
					output[i] = v / MathF.Sqrt(lenSq);
				else
					output[i] = Vector2.Zero;
			}
		}
	}
	/**
     * A 2-dimensional euclidean vector with double precision.
     * Implementation based on the Vector2 template from Artery Engine.
     * @author Viktor Chlumsky
     */
	/*public struct Vector2 : IEquatable<Vector2>
    {
        public double X, Y;

        public static Vector2 Zero { get; } = new (0, 0);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Vector2(double val)
        {
            X = val;
            Y = val;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Vector2(double x, double y)
        {
            X = x;
            Y = y;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Equals(Vector2 other)
        {
            return X.Equals(other.X) && Y.Equals(other.Y);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            return obj is Vector2 other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (X.GetHashCode() * 397) ^ Y.GetHashCode();
            }
        }

        /// Returns the vector's length.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public double Length()
        {
            return Math.Sqrt(X * X + Y * Y);
        }

        /// Returns the normalized vector - one that has the same direction but unit length.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Vector2 Normalize(bool allowZero = false) {
            double len = Length();
            if (len != 0)
                return new (X / len, Y / len);
            return new (0, allowZero? 0.0: 1.0);
        }

    /// Returns a vector with unit length that is orthogonal to this one
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Vector2 GetOrthonormal(bool polarity = true, bool allowZero = false) 
        {
            double len = Length();
            if (len != 0)
                return polarity? new (-Y/len, X/len) : new (Y/len, -X/len);
            return polarity? new (0, allowZero ? 0 : 1) : new (0, -(allowZero? 0: 1));
        }


    /// Returns a vector with the same length that is orthogonal to this one.
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Vector2 GetOrthogonal(bool polarity = true) {
            return polarity? new (-Y, X) : new (Y, -X);
        }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator !(Vector2 lhs)
        {
            return lhs.X == 0 && lhs.Y == 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator ==(Vector2 lhs, Vector2 rhs)
        {
            return lhs.X == rhs.X && lhs.Y == rhs.Y;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator !=(Vector2 lhs, Vector2 rhs)
        {
            return lhs.X != rhs.X || lhs.Y != rhs.Y;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2 operator +(Vector2 lhs)
        {
            return new Vector2(lhs.X, lhs.Y);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2 operator -(Vector2 lhs)
        {
            return new Vector2(-lhs.X, -lhs.Y);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2 operator +(Vector2 lhs, Vector2 rhs)
        {
            return new Vector2(lhs.X + rhs.X, lhs.Y + rhs.Y);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2 operator -(Vector2 lhs, Vector2 rhs)
        {
            return new Vector2(lhs.X - rhs.X, lhs.Y - rhs.Y);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2 operator *(Vector2 lhs, Vector2 rhs)
        {
            return new Vector2(lhs.X * rhs.X, lhs.Y * rhs.Y);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2 operator /(Vector2 lhs, Vector2 rhs)
        {
            return new Vector2(lhs.X / rhs.X, lhs.Y / rhs.Y);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2 operator *(Vector2 lhs, double value)
        {
            return new Vector2(lhs.X * value, lhs.Y * value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2 operator /(Vector2 lhs, double value)
        {
            return new Vector2(lhs.X / value, lhs.Y / value);
        }

        /// Dot product of two vectors.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double Dot(Vector2 a, Vector2 b)
        {
            return a.X * b.X + a.Y * b.Y;
        }

        /// A special version of the cross product for 2D vectors (returns scalar value).
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double Cross(Vector2 a, Vector2 b)
        {
            return a.X * b.Y - a.Y * b.X;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2 operator *(double value, Vector2 vector)
        {
            return new Vector2(value * vector.X, value * vector.Y);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2 operator /(double value, Vector2 vector)
        {
            return new Vector2(value / vector.X, value / vector.Y);
        }
    }*/
}