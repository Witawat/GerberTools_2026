using System;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace TilingLibrary.Compatibility
{
    /// <summary>
    /// ImageSharp-based DirectBitmap replacement providing fast pixel access.
    /// This class provides similar API to the System.Drawing DirectBitmap but uses ImageSharp internally.
    /// </summary>
    public class DirectBitmap : IDisposable
    {
        private Image<Rgba32> _image;
        public bool Disposed { get; private set; }
        public int Height { get; private set; }
        public int Width { get; private set; }

        /// <summary>
        /// Gets the underlying ImageSharp image for advanced operations.
        /// </summary>
        public Image<Rgba32> Image => _image;

        public DirectBitmap(int width, int height)
        {
            Width = width;
            Height = height;
            _image = new Image<Rgba32>(width, height);
        }

        /// <summary>
        /// Sets a pixel at the specified coordinates. 
        /// Note: For bulk operations, use ProcessPixels for better performance.
        /// </summary>
        public void SetPixelFast(int x, int y, Color colour)
        {
            if (x < 0 || x >= Width || y < 0 || y >= Height)
                return;

            _image[x, y] = colour.ToPixel<Rgba32>();
        }

        /// <summary>
        /// Gets a pixel at the specified coordinates.
        /// Note: For bulk operations, use ProcessPixels for better performance.
        /// </summary>
        public Color GetPixelFast(int x, int y)
        {
            if (x < 0 || x >= Width || y < 0 || y >= Height)
                return Color.Transparent;

            var pixel = _image[x, y];
            return Color.FromRgba(pixel.R, pixel.G, pixel.B, pixel.A);
        }

        /// <summary>
        /// Process all pixels using a callback. This is much faster than individual GetPixelFast/SetPixelFast calls.
        /// </summary>
        /// <param name="processor">Action that receives a row span and the y coordinate</param>
        public void ProcessPixels(Action<Span<Rgba32>, int> processor)
        {
            _image.ProcessPixelRows(accessor =>
            {
                for (int y = 0; y < accessor.Height; y++)
                {
                    Span<Rgba32> row = accessor.GetRowSpan(y);
                    processor(row, y);
                }
            });
        }

        /// <summary>
        /// Process a specific region of pixels.
        /// </summary>
        public void ProcessPixelRegion(int startX, int startY, int width, int height, Action<Span<Rgba32>, int, int> processor)
        {
            _image.ProcessPixelRows(accessor =>
            {
                int endY = Math.Min(startY + height, accessor.Height);
                int endX = Math.Min(startX + width, accessor.Width);

                for (int y = startY; y < endY; y++)
                {
                    Span<Rgba32> row = accessor.GetRowSpan(y);
                    Span<Rgba32> region = row.Slice(startX, endX - startX);
                    processor(region, startX, y);
                }
            });
        }

        /// <summary>
        /// Saves the image to a file.
        /// </summary>
        public void Save(string path)
        {
            _image.SaveAsPng(path);
        }

        public void Dispose()
        {
            if (Disposed) return;
            Disposed = true;
            _image?.Dispose();
        }
    }
}
