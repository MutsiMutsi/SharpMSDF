using System;
using System.Numerics;
using System.Runtime.CompilerServices;
using Typography.OpenFont;

namespace SharpMSDF.Core
{
	public static class Arithmetic
	{

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static float Mix(float a, float b, float weight)
		{
			return (float)((1.0f - weight) * a + weight * b);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static int NonZeroSign(float n)
		{
			return n > 0 ? 1 : -1;
		}
	}
}