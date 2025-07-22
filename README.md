<h1 align="center">SharpMSDF</h1>
.NET port of Chlumsky's msdfgen + msdf-atlas-gen with OpenType loader all in pure C# (no native dlls included) 


## Features :
- ✅ No native dependencies (should work on any platform that works with .NET 8.0+).
- ✅ OpenType (ttf/otf/..) font loader.
- ✅ Glyph MSDF generator.
- ⏳ Uses various libraries (MonoGame, KNI and Skia/System.Drawing) for geometry and bitmaping.
- ⏳ MSDF Atlas generator.
- ⏳ Dotnet tool for MSDF Atlas generation.
- ⏳ Better optimized codebase.

## Licenses:

MIT License 2025 FenzDev

MIT License 2014-2025, Viktor Chlumsky

(LayoutFarm/Typography licenses can be seen in header of its source files) 


## References:

OpenType: 
https://github.com/LayoutFarm/Typography/tree/master/Typography.OpenFont

MSDF:
https://github.com/Chlumsky/msdfgen
https://github.com/Chlumsky/msdf-atlas-gen
https://github.com/DWVoid/Msdfgen.Net/blob/master/Msdfgen.IO/ImportFont.cs