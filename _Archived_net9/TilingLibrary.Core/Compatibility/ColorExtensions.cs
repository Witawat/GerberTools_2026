using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace TilingLibrary.Compatibility
{
    /// <summary>
    /// Extension methods for ImageSharp Color to provide System.Drawing compatibility
    /// </summary>
    public static class ColorExtensions
    {
        /// <summary>
        /// Gets the brightness (luminance) of the color, similar to System.Drawing.Color.GetBrightness()
        /// Returns a value between 0.0 (black) and 1.0 (white)
        /// </summary>
        public static float GetBrightness(this Color color)
        {
            var pixel = color.ToPixel<Rgba32>();
            float r = pixel.R / 255.0f;
            float g = pixel.G / 255.0f;
            float b = pixel.B / 255.0f;

            float max = Math.Max(r, Math.Max(g, b));
            float min = Math.Min(r, Math.Min(g, b));

            return (max + min) / 2.0f;
        }

        /// <summary>
        /// Gets the hue of the color in degrees (0-360)
        /// </summary>
        public static float GetHue(this Color color)
        {
            var pixel = color.ToPixel<Rgba32>();
            float r = pixel.R / 255.0f;
            float g = pixel.G / 255.0f;
            float b = pixel.B / 255.0f;

            float max = Math.Max(r, Math.Max(g, b));
            float min = Math.Min(r, Math.Min(g, b));
            float delta = max - min;

            if (delta == 0)
                return 0;

            float hue;
            if (max == r)
                hue = ((g - b) / delta) % 6;
            else if (max == g)
                hue = (b - r) / delta + 2;
            else
                hue = (r - g) / delta + 4;

            hue *= 60;
            if (hue < 0)
                hue += 360;

            return hue;
        }

        /// <summary>
        /// Gets the saturation of the color (0.0 to 1.0)
        /// </summary>
        public static float GetSaturation(this Color color)
        {
            var pixel = color.ToPixel<Rgba32>();
            float r = pixel.R / 255.0f;
            float g = pixel.G / 255.0f;
            float b = pixel.B / 255.0f;

            float max = Math.Max(r, Math.Max(g, b));
            float min = Math.Min(r, Math.Min(g, b));
            float delta = max - min;

            if (max == 0)
                return 0;

            return delta / max;
        }
    }
}
