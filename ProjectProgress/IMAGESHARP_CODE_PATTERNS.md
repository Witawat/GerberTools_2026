# ImageSharp Migration Code Patterns

Quick reference guide for common System.Drawing to ImageSharp conversions.

---

## Table of Contents
1. [Using Statements](#using-statements)
2. [Color Operations](#color-operations)
3. [Image Creation & Loading](#image-creation--loading)
4. [Drawing Operations](#drawing-operations)
5. [Pixel Access](#pixel-access)
6. [Transformations](#transformations)
7. [Text & Fonts](#text--fonts)
8. [Image Saving](#image-saving)
9. [Advanced Operations](#advanced-operations)

---

## Using Statements

### System.Drawing
```csharp
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing.Text;
```

### ImageSharp
```csharp
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Drawing;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.Fonts;
using System.Numerics; // For Matrix3x2

// Type aliases
using Bitmap = SixLabors.ImageSharp.Image<SixLabors.ImageSharp.PixelFormats.Rgba32>;
using Color = SixLabors.ImageSharp.Color;
using PointF = SixLabors.ImageSharp.PointF;
using SizeF = SixLabors.ImageSharp.SizeF;
using RectangleF = SixLabors.ImageSharp.RectangleF;
```

---

## Color Operations

### Creating Colors

```csharp
// System.Drawing → ImageSharp

// RGB
Color.FromArgb(255, 128, 0)
→ Color.FromRgb(255, 128, 0)

// RGBA
Color.FromArgb(200, 255, 128, 0)
→ Color.FromRgba(255, 128, 0, 200)  // Note: parameter order changed!

// Named colors
Color.Red
→ Color.Red  // Same

// Transparent
Color.Transparent
→ Color.Transparent  // Same

// From hex
Color.FromArgb(0xFF8000)
→ Color.ParseHex("#FF8000")
```

### Accessing Color Components

```csharp
// System.Drawing → ImageSharp

// Direct access (doesn't work in ImageSharp)
color.R
color.G
color.B
color.A

// ImageSharp way
var pixel = color.ToPixel<Rgba32>();
pixel.R
pixel.G
pixel.B
pixel.A

// Or
var (r, g, b, a) = color.ToPixel<Rgba32>();
```

### Color Manipulation

```csharp
// System.Drawing → ImageSharp

// Brightness
color.GetBrightness()
→ color.ToPixel<Rgba32>().GetBrightness()  // Custom extension needed

// Custom extension:
public static float GetBrightness(this Rgba32 pixel)
{
    float r = pixel.R / 255f;
    float g = pixel.G / 255f;
    float b = pixel.B / 255f;
    float max = Math.Max(r, Math.Max(g, b));
    float min = Math.Min(r, Math.Min(g, b));
    return (max + min) / 2f;
}
```

---

## Image Creation & Loading

### Creating New Images

```csharp
// System.Drawing → ImageSharp

// Basic creation
new Bitmap(width, height)
→ new Image<Rgba32>(width, height)

// With pixel format
new Bitmap(width, height, PixelFormat.Format32bppArgb)
→ new Image<Rgba32>(width, height)  // Rgba32 is equivalent

// With background color
var bmp = new Bitmap(width, height);
using (var g = Graphics.FromImage(bmp))
{
    g.Clear(Color.White);
}
→
var img = new Image<Rgba32>(width, height, Color.White);
```

### Loading Images

```csharp
// System.Drawing → ImageSharp

// From file
Bitmap.FromFile(path)
→ Image.Load<Rgba32>(path)

// From stream
new Bitmap(stream)
→ Image.Load<Rgba32>(stream)

// With specific decoder
Image.Load<Rgba32>(path, new PngDecoder())
```

---

## Drawing Operations

### Getting Drawing Context

```csharp
// System.Drawing → ImageSharp

// Get graphics from image
Graphics g = Graphics.FromImage(bitmap);
g.DrawLine(pen, x1, y1, x2, y2);
g.Dispose();

// ImageSharp - using custom GraphicsInterface
var g = new ImageSharpGraphicsInterface(image);
g.DrawLine(pen, new PointF(x1, y1), new PointF(x2, y2));

// Or directly with Mutate
image.Mutate(ctx => ctx.DrawLine(pen, new PointF(x1, y1), new PointF(x2, y2)));
```

### Basic Shapes

```csharp
// System.Drawing → ImageSharp

// Line
g.DrawLine(pen, x1, y1, x2, y2)
→ image.Mutate(ctx => ctx.DrawLine(pen, new PointF(x1, y1), new PointF(x2, y2)))

// Rectangle
g.DrawRectangle(pen, x, y, w, h)
→ image.Mutate(ctx => ctx.Draw(pen, new RectangleF(x, y, w, h)))

// Fill rectangle
g.FillRectangle(brush, x, y, w, h)
→ image.Mutate(ctx => ctx.Fill(brush, new RectangleF(x, y, w, h)))

// Ellipse
g.DrawEllipse(pen, x, y, w, h)
→ image.Mutate(ctx => ctx.Draw(pen, new EllipsePolygon(x + w/2, y + h/2, w/2, h/2)))

// Polygon
g.DrawPolygon(pen, points)
→ image.Mutate(ctx => ctx.DrawPolygon(pen, points))

// Fill polygon
g.FillPolygon(brush, points)
→ image.Mutate(ctx => ctx.FillPolygon(brush, points))
```

### Pens and Brushes

```csharp
// System.Drawing → ImageSharp

// Pen
new Pen(Color.Red, 2.0f)
→ new SolidPen(Color.Red, 2.0f)

// Brush
new SolidBrush(Color.Blue)
→ new SolidBrush(Color.Blue)  // Use custom type from Primitives

// In ImageSharp.Drawing.Processing:
new SixLabors.ImageSharp.Drawing.Processing.SolidBrush(Color.Blue)
```

### Clear/Fill Background

```csharp
// System.Drawing → ImageSharp

g.Clear(Color.White)
→ image.Mutate(ctx => ctx.Clear(Color.White))
```

---

## Pixel Access

### Individual Pixel Access (Slow)

```csharp
// System.Drawing → ImageSharp

// Get pixel
Color c = bitmap.GetPixel(x, y)
→ Color c = image[x, y].ToScaledVector4().ToColor()
// Or simpler:
→ Rgba32 pixel = image[x, y]

// Set pixel
bitmap.SetPixel(x, y, color)
→ image[x, y] = color.ToPixel<Rgba32>()
```

### Bulk Pixel Access (Fast)

```csharp
// System.Drawing → ImageSharp

// LockBits pattern
BitmapData data = bitmap.LockBits(
    new Rectangle(0, 0, bitmap.Width, bitmap.Height),
    ImageLockMode.ReadWrite,
    PixelFormat.Format32bppArgb);

unsafe
{
    byte* ptr = (byte*)data.Scan0;
    int stride = data.Stride;
    
    for (int y = 0; y < bitmap.Height; y++)
    {
        byte* row = ptr + (y * stride);
        for (int x = 0; x < bitmap.Width; x++)
        {
            byte b = row[x * 4];
            byte g = row[x * 4 + 1];
            byte r = row[x * 4 + 2];
            byte a = row[x * 4 + 3];
            
            // Process pixel
            
            row[x * 4] = b;
            row[x * 4 + 1] = g;
            row[x * 4 + 2] = r;
            row[x * 4 + 3] = a;
        }
    }
}

bitmap.UnlockBits(data);

// ImageSharp equivalent
image.ProcessPixelRows(accessor =>
{
    for (int y = 0; y < accessor.Height; y++)
    {
        Span<Rgba32> row = accessor.GetRowSpan(y);
        for (int x = 0; x < row.Length; x++)
        {
            Rgba32 pixel = row[x];
            
            // Process pixel
            byte r = pixel.R;
            byte g = pixel.G;
            byte b = pixel.B;
            byte a = pixel.A;
            
            row[x] = new Rgba32(r, g, b, a);
        }
    }
});
```

### DirectBitmap Pattern

```csharp
// Custom DirectBitmap class for ImageSharp
public class DirectBitmap : IDisposable
{
    private Image<Rgba32> _image;
    
    public DirectBitmap(int width, int height)
    {
        _image = new Image<Rgba32>(width, height);
    }
    
    public int Width => _image.Width;
    public int Height => _image.Height;
    public Image<Rgba32> Image => _image;
    
    public Color GetPixelFast(int x, int y)
    {
        var pixel = _image[x, y];
        return Color.FromRgba(pixel.R, pixel.G, pixel.B, pixel.A);
    }
    
    public void SetPixelFast(int x, int y, Color color)
    {
        _image[x, y] = color.ToPixel<Rgba32>();
    }
    
    public void Dispose()
    {
        _image?.Dispose();
    }
}
```

---

## Transformations

### Matrix Operations

```csharp
// System.Drawing → ImageSharp

// Using System.Drawing.Drawing2D.Matrix
Matrix m = new Matrix();
m.RotateAt(45, new PointF(100, 100));
m.Translate(10, 20);
m.TransformPoints(points);

// Using System.Numerics.Matrix3x2
var m = Matrix3x2.Identity;
var center = new Vector2(100, 100);
var translation1 = Matrix3x2.CreateTranslation(-center);
var rotation = Matrix3x2.CreateRotation(45 * (float)(Math.PI / 180));
var translation2 = Matrix3x2.CreateTranslation(center);
m = translation1 * rotation * translation2;
m = Matrix3x2.CreateTranslation(10, 20) * m;

// Transform points
for (int i = 0; i < points.Length; i++)
{
    var p = new Vector2(points[i].X, points[i].Y);
    p = Vector2.Transform(p, m);
    points[i] = new PointF(p.X, p.Y);
}
```

### Graphics Transformations

```csharp
// System.Drawing → ImageSharp

// Rotate
g.RotateTransform(45)
→ image.Mutate(ctx => ctx.Rotate(45))

// Translate
g.TranslateTransform(10, 20)
→ image.Mutate(ctx => ctx.Transform(Matrix3x2.CreateTranslation(10, 20)))

// Scale
g.ScaleTransform(2.0f, 2.0f)
→ image.Mutate(ctx => ctx.Transform(Matrix3x2.CreateScale(2.0f, 2.0f)))

// Combined transformations
var transform = Matrix3x2.CreateRotation(45 * (float)(Math.PI / 180)) *
                Matrix3x2.CreateTranslation(10, 20);
image.Mutate(ctx => ctx.Transform(transform));
```

---

## Text & Fonts

### Font Creation

```csharp
// System.Drawing → ImageSharp

// Basic font
new Font("Arial", 12)
→ SystemFonts.CreateFont("Arial", 12)

// With style
new Font("Arial", 12, FontStyle.Bold)
→ SystemFonts.CreateFont("Arial", 12, FontStyle.Bold)

// Font family
new FontFamily("Arial")
→ SystemFonts.Find("Arial") ?? SystemFonts.Families.First()
```

### Measuring Text

```csharp
// System.Drawing → ImageSharp

// Measure string
SizeF size = g.MeasureString(text, font)
→ 
var font = SystemFonts.CreateFont("Arial", 12);
var options = new TextOptions(font);
FontRectangle rect = TextMeasurer.Measure(text, options);
SizeF size = new SizeF(rect.Width, rect.Height);
```

### Drawing Text

```csharp
// System.Drawing → ImageSharp

// Draw string
g.DrawString(text, font, brush, x, y)
→
var font = SystemFonts.CreateFont("Arial", 12);
var options = new RichTextOptions(font)
{
    Origin = new PointF(x, y)
};
image.Mutate(ctx => ctx.DrawText(options, text, brush.Color));
```

### GraphicsPath for Text

```csharp
// System.Drawing → ImageSharp

// Text to path
GraphicsPath path = new GraphicsPath();
path.AddString(text, fontFamily, (int)FontStyle.Regular, 
               fontSize, location, StringFormat.GenericDefault);
g.DrawPath(pen, path);

// ImageSharp equivalent
var font = SystemFonts.CreateFont(fontFamily.Name, fontSize);
var glyphs = TextBuilder.GenerateGlyphs(text, new PointF(x, y), 
                                        new TextOptions(font));
var pathBuilder = new PathBuilder();
foreach (var glyph in glyphs)
{
    pathBuilder.AddPath(glyph.Path);
}
var path = pathBuilder.Build();
image.Mutate(ctx => ctx.Draw(pen, path));
```

---

## Image Saving

### Save to File

```csharp
// System.Drawing → ImageSharp

// PNG
bitmap.Save(path, ImageFormat.Png)
→ image.SaveAsPng(path)

// JPEG
bitmap.Save(path, ImageFormat.Jpeg)
→ image.SaveAsJpeg(path)

// With quality
var encoder = new JpegEncoder { Quality = 90 };
bitmap.Save(path, encoder)
→ image.SaveAsJpeg(path, new JpegEncoder { Quality = 90 })

// BMP
bitmap.Save(path, ImageFormat.Bmp)
→ image.SaveAsBmp(path)

// GIF
bitmap.Save(path, ImageFormat.Gif)
→ image.SaveAsGif(path)
```

### Save to Stream

```csharp
// System.Drawing → ImageSharp

bitmap.Save(stream, ImageFormat.Png)
→ image.SaveAsPng(stream)
```

---

## Advanced Operations

### Compositing Modes

```csharp
// System.Drawing → ImageSharp

// CompositingMode
g.CompositingMode = CompositingMode.SourceOver
→ 
// Use GraphicsOptions in DrawingOptions
var options = new DrawingOptions
{
    GraphicsOptions = new GraphicsOptions
    {
        BlendPercentage = 1.0f  // Full opacity
    }
};

// CompositingQuality
g.CompositingQuality = CompositingQuality.HighQuality
→ 
// ImageSharp always uses high quality
```

### Smoothing/Antialiasing

```csharp
// System.Drawing → ImageSharp

g.SmoothingMode = SmoothingMode.AntiAlias
→
var options = new DrawingOptions
{
    GraphicsOptions = new GraphicsOptions
    {
        Antialias = true
    }
};

// Or in custom GraphicsInterface
public SmoothingMode SmoothingMode { get; set; }

private DrawingOptions GetOptions()
{
    return new DrawingOptions
    {
        GraphicsOptions = new GraphicsOptions
        {
            Antialias = (SmoothingMode == SmoothingMode.AntiAlias)
        }
    };
}
```

### Interpolation Mode

```csharp
// System.Drawing → ImageSharp

g.InterpolationMode = InterpolationMode.HighQualityBicubic
→
image.Mutate(ctx => ctx.Resize(new ResizeOptions
{
    Sampler = KnownResamplers.Bicubic
}));

// Mapping:
// InterpolationMode.NearestNeighbor → KnownResamplers.NearestNeighbor
// InterpolationMode.Bilinear → KnownResamplers.Triangle
// InterpolationMode.Bicubic → KnownResamplers.Bicubic
// InterpolationMode.HighQualityBicubic → KnownResamplers.Bicubic
```

### Clipping

```csharp
// System.Drawing → ImageSharp

// Set clip region
g.SetClip(new Rectangle(x, y, w, h))
→
// Use Clip() in processing pipeline
image.Mutate(ctx => ctx.Clip(new RectangleF(x, y, w, h)));
```

### Drawing Images

```csharp
// System.Drawing → ImageSharp

// Draw image
g.DrawImage(sourceImage, x, y)
→
image.Mutate(ctx => ctx.DrawImage(sourceImage, new Point(x, y), 1.0f));

// Draw image with size
g.DrawImage(sourceImage, x, y, width, height)
→
image.Mutate(ctx => 
{
    var resized = sourceImage.Clone(c => c.Resize(width, height));
    ctx.DrawImage(resized, new Point(x, y), 1.0f);
});
```

---

## Common Gotchas

### 1. Color Parameter Order
```csharp
// System.Drawing: ARGB
Color.FromArgb(alpha, red, green, blue)

// ImageSharp: RGBA
Color.FromRgba(red, green, blue, alpha)  // Different order!
```

### 2. Coordinate Systems
```csharp
// Both use top-left origin, but be aware of:
// - Font baseline differences
// - Text positioning may need adjustment
```

### 3. Disposal
```csharp
// System.Drawing - must dispose
using (var bitmap = new Bitmap(100, 100))
using (var g = Graphics.FromImage(bitmap))
{
    // ...
}

// ImageSharp - also must dispose
using (var image = new Image<Rgba32>(100, 100))
{
    // ...
}
```

### 4. Performance
```csharp
// Slow - individual pixel access
for (int y = 0; y < height; y++)
    for (int x = 0; x < width; x++)
        image[x, y] = color;

// Fast - bulk processing
image.ProcessPixelRows(accessor =>
{
    for (int y = 0; y < accessor.Height; y++)
    {
        Span<Rgba32> row = accessor.GetRowSpan(y);
        row.Fill(color.ToPixel<Rgba32>());
    }
});
```

---

## Migration Checklist for Each File

When migrating a file, check:

- [ ] Update using statements
- [ ] Replace Bitmap with Image<Rgba32>
- [ ] Replace Graphics with GraphicsInterface or Mutate
- [ ] Replace Color operations (check ARGB vs RGBA)
- [ ] Replace pixel access with ProcessPixelRows
- [ ] Replace Matrix with Matrix3x2
- [ ] Replace Font operations
- [ ] Replace Save operations
- [ ] Test visual output
- [ ] Check performance

---

**Reference:** See `TILINGLIBRARY_MIGRATION_PLAN.md` for full migration strategy  
**Example:** See `GerberLibrary.Core/` for completed migration
