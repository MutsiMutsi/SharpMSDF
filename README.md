<h1 align="center">SharpMSDF</h1>

.NET port of Chlumsky's [msdfgen](https://github.com/Chlumsky/msdfgen) and [msdf-atlas-gen](https://github.com/Chlumsky/msdfgen) with OpenType loader all in pure C# (no native dlls included) 

More specifically version 1.12.1 of msdfgen

## Features :
#### Done:
- ✅ No native dependencies (should work on any platform that works with .NET 8.0+).
- ✅ OpenType (ttf/otf/..) font loader.
- ✅ SDF/PSDF/MSDF/MTSDF generator.
- ✅ Export to bmp and png image formats.
#### Work in progress:
- ⏳ MSDF Atlas generator.
- ⏳ Compability with MonoGame and KNI in terms of Texture map and Geometry.
#### Idle (Request if needed via Issue/PR): 
- ⏸ Generation from a [Shape Description](https://github.com/Chlumsky/msdfgen?tab=readme-ov-file#shape-description-syntax).
- ⏸ Dotnet tool for MSDF Atlas generation.


## Licenses:

MIT License 2025 FenzDev

MIT License 2014-2025, Viktor Chlumsky

(LayoutFarm/Typography licenses can be seen in header of its source files) 


## References:

OpenType: 
- https://github.com/LayoutFarm/Typography/tree/master/Typography.OpenFont

MSDF:
- https://github.com/Chlumsky/msdfgen
- https://github.com/Chlumsky/msdf-atlas-gen
- https://github.com/DWVoid/Msdfgen.Net