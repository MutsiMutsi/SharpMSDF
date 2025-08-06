using SharpMSDF.Core;

namespace SharpMSDF.Atlas
{
	public static class GlyphGenerators
	{

		public static void Msdf(BitmapView output, GlyphGeometry glyph, GeneratorAttributes attribs)
		{

			// Calculate required memory size
			var shape = glyph.GetShape();
			int requiredMemory = MSDFGen.GetRequiredWorkingMemorySize(shape);
			Span<byte> workingMemory = stackalloc byte[requiredMemory]; // Or rent from ArrayPool


			MSDFGeneratorConfig config = attribs.Config;

			// Generate MSDF using the working memory
			MSDFGen.GenerateMSDF(output, shape, new(glyph.GetBoxProjection(), new(glyph.GetBoxRange())), workingMemory, config);
		}
	}
}
