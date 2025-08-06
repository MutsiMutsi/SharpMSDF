using SharpMSDF.Core;

namespace SharpMSDF.Atlas
{
	public static class GlyphGenerators
	{

		public static void Msdf(BitmapView output, GlyphGeometry glyph, GeneratorAttributes attribs)
		{
			MSDFGeneratorConfig config = attribs.Config;
			MSDFGen.GenerateMSDF(output, glyph.GetShape(), new(glyph.GetBoxProjection(), new(glyph.GetBoxRange())), config);
		}
	}
}
