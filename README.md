<h1 align="center"><img align="center" src=".media/icon-msdf-atlas-gen.png"/> SharpMSDF <img align="center" src=".media/icon-msdfgen.png"/></h1>

C#/.NET Port of Chlumsky's [msdfgen](https://github.com/Chlumsky/msdfgen) and [msdf-atlas-gen](https://github.com/Chlumsky/msdfgen) 
written in pure C# with no native dlls included !


More specifically version 1.12.1 of msdfgen

## Features
- ✅ No native dependencies (should work on any platform that works with .NET 8.0+).
- ✅ OpenType (ttf/otf/..) font loader.
- ✅ SDF/PSDF/MSDF/MTSDF generator.
- ✅ bmp and png image formats Exporter.
- ✅ MSDF Atlas generator.
- ✅ Dynamic Atlas (Load glyphs on-the-fly when needed).

## Usage Examples
### Generate one glyph
```cs
// Load the ttf/otf font
var font = FontImporter.LoadFont("C:\\Windows\\Fonts\\ariblk.ttf");

// Set some generation parameters 
int scale = 64;
double pxrange = 6.0;
double angleThereshold = 3.0;

// Load the glyph
var shape = FontImporter.LoadGlyph(font, '#', FontCoordinateScaling.EmNormalized);
var msdf = new Bitmap<float>(scale, scale, 3);

shape.OrientContours(); // This will fix orientation of the windings
shape.Normalize(); // Normalize the Shape geometry for distance field generation.
EdgeColorings.InkTrap(shape, angleThereshold); // Assign colors to the edges of the shape, we use InkTrap technique here.

// range = pxrange / scale
var distMap = new DistanceMapping(new(pxrange / scale));
var transformation = new SDFTransformation(new Projection(new(scale), new(0)), distMap);
                                                        //     ^ Scale    ^ Translation  
// Generate msdf
MSDFGen.GenerateMSDF(
    msdf,
    shape,
    transformation
);

// Save msdf output
Png.SavePng(msdf, "output.png");

// Save a rendering preview
var rast = new Bitmap<float>(1024, 1024);
Render.RenderSdf(rast, msdf, pxrange);
Png.SavePng(rast, "render.png");

```
### Generate full atlas
```cs
List<GlyphGeometry> glyphs = new (font.GlyphCount);
// FontGeometry is a helper class that loads a set of glyphs from a single font.
// It can also be used to get additional font metrics, kerning information, etc.
FontGeometry fontGeometry = new (glyphs);
// Load a set of character glyphs:
// The second argument can be ignored unless you mix different font sizes in one atlas.
// In the last argument, you can specify a charset other than ASCII.
// To load specific glyph indices, use loadGlyphs instead.
fontGeometry.LoadCharset(font, 1.0, Charset.ASCII);
// Apply MSDF edge coloring. See EdgeColorings for other coloring strategies.
const double maxCornerAngle = 3.0;
for (var g = 0; g < glyphs.Count; g++)
{
    glyphs[g].GetShape().OrientContours();
    glyphs[g].EdgeColoring(EdgeColorings.InkTrap, maxCornerAngle, 0);
}
// TightAtlasPacker class computes the layout of the atlas.
TightAtlasPacker packer = new();
// Set atlas parameters:
// setDimensions or setDimensionsConstraint to find the best value
packer.SetDimensionsConstraint(DimensionsConstraint.Square);
// setScale for a fixed scale or setMinimumScale to use the largest that fits
packer.SetMinimumScale(64.0);
// setPixelRange or setUnitRange
packer.SetPixelRange(new DoubleRange(6.0));
packer.SetMiterLimit(1.0);
packer.SetOriginPixelAlignment(false, true);
// Compute atlas layout - pack glyphs
packer.Pack(ref glyphs);
// Get final atlas dimensions
packer.GetDimensions(out int width, out int height);

//Gen function
GeneratorFunction<float> msdfGen = GlyphGenerators.Msdf;
            
// The ImmediateAtlasGenerator class facilitates the generation of the atlas bitmap.
ImmediateAtlasGenerator <
        float, // pixel type of buffer for individual glyphs depends on generator function
        BitmapAtlasStorage<byte> // class that stores the atlas bitmap
        // For example, a custom atlas storage class that stores it in VRAM can be used.
    > generator = new(width, height, 3, msdfGen);
// GeneratorAttributes can be modified to change the generator's default settings.
GeneratorAttributes attributes = new();
generator.SetAttributes(attributes);
generator.SetThreadCount(4);
// Generate atlas bitmap
generator.Generate(glyphs);
// The atlas bitmap can now be retrieved via atlasStorage as a BitmapConstRef.
// The glyphs array (or fontGeometry) contains positioning data for typesetting text.

// Save the atlas as png if wanted
Png.SavePng(generator.Storage.Bitmap, "atlas.png");
```

### Generate dynamic atlas
```cs
// Atlas parameters
const double pixelRange = 6.0;
const double glyphScale = 64.0;
const double miterLimit = 2.0;
const double maxCornerAngle = 3.0;

// Initialize
List<GlyphGeometry> glyphs = new(font.GlyphCount);
FontGeometry fontGeometry = new(glyphs);
DynamicAtlas<ImmediateAtlasGenerator<float, BitmapAtlasStorage<byte>>> myDynamicAtlas = new();
myDynamicAtlas.Generator = new(3, GlyphGenerators.Msdf);
myDynamicAtlas.Packer = new();
            
// ...

// When glyphs(s) should be added
{
    ReadOnlySpan<char> chars = "*string containing the glyphs to be added*";

    Charset charset = new();
    int prevEndMark = glyphs.Count;
    for (int c = 0; c < chars.Length; ++c)
        charset.Add(chars[c]);
    fontGeometry.LoadCharset(font, 1.0, charset);

    for (int g = prevEndMark; g < glyphs.Count; ++g)
    {
        var glyph = glyphs[g];
        // Preprocess windings
        glyph.GetShape().OrientContours();
        // Apply MSDF edge coloring. See EdgeColorings for other coloring strategies.
        glyph.EdgeColoring(EdgeColorings.InkTrap, maxCornerAngle, 0);
        // Finalize glyph box scale based on the parameters
        glyph.WrapBox(new() { Scale = glyphScale, Range = new(pixelRange / glyphScale), MiterLimit = miterLimit });

        glyphs[g] = glyph;
    }

    // Add glyphs to atlas - invokes the underlying atlas generator
    // Adding multiple glyphs at once may improve packing efficiency.
    var newGlyphs = glyphs[prevEndMark..];
	var changeFlags = myDynamicAtlas.Add(newGlyphs);
	for (int i = 0; i < newGlyphs.Count; ++i)
	{
		glyphs[prevEndMark + i] = newGlyphs[i];
	}
    if ((changeFlags & ChangeFlag.Resized) != 0)
    {
        // Recreate texture slot with new size when resized for example
    }

    // You can store bitmap data to the texture slot with this
    var bitmap = myDynamicAtlas.Generator.Storage.Bitmap;

    // ...
}
```
## Feedback
This port doesn't cover the whole project, If you find any bugs or want to add some missing stuff, you can post an Issue or PR.

## Licenses

MIT License 2025 FenzDev

MIT License 2014-2025, Viktor Chlumsky

(LayoutFarm/Typography licenses can be seen in header of its source files) 

## References

OpenType: 
- https://github.com/LayoutFarm/Typography/tree/master/Typography.OpenFont

MSDF:
- https://github.com/Chlumsky/msdfgen
- https://github.com/Chlumsky/msdf-atlas-gen
- https://github.com/DWVoid/Msdfgen.Net