
namespace SharpMSDF.Atlas
{

    /// Type of atlas image contents
    enum ImageType
    {
        /// Rendered glyphs without anti-aliasing (two colors only)
        HARD_MASK,
        /// Rendered glyphs with anti-aliasing
        SOFT_MASK,
        /// Signed (true) distance field
        SDF,
        /// Signed perpendicular distance field
        PSDF,
        /// Multi-channel signed distance field
        MSDF,
        /// Multi-channel & true signed distance field
        MTSDF
    };

    /// Atlas image encoding
    enum ImageFormat
    {
        UNSPECIFIED,
        PNG,
        BMP,
        TIFF,
        RGBA,
        FL32,
        TEXT,
        TEXT_FLOAT,
        BINARY,
        BINARY_FLOAT,
        BINARY_FLOAT_BE
    };

    /// Glyph identification
    enum GlyphIdentifierType
    {
        GLYPH_INDEX,
        UNICODE_CODEPOINT
    };

    /// Direction of the Y-axis
    enum YDirection
    {
        BOTTOM_UP,
        TOP_DOWN
    };

    /// The method of computing the layout of the atlas
    enum PackingStyle
    {
        TIGHT,
        GRID
    };

    /// Constraints for the atlas's dimensions - see size selectors for more info
    enum DimensionsConstraint
    {
        NONE,
        SQUARE,
        EVEN_SQUARE,
        MULTIPLE_OF_FOUR_SQUARE,
        POWER_OF_TWO_RECTANGLE,
        POWER_OF_TWO_SQUARE
    };

}
