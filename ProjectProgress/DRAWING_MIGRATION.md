# System.Drawing to ImageSharp Migration Guide

## Common Replacements

### Namespaces
```csharp
// OLD
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;

// NEW
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Drawing;
using SixLabors.ImageSharp.Drawing.Processing;
```

### Types
```csharp
// Bitmap
System.Drawing.Bitmap → Image<Rgba32>

// Color
System.Drawing.Color → SixLabors.ImageSharp.Color

// Point/PointF
System.Drawing.Point → SixLabors.ImageSharp.Point
System.Drawing.PointF → SixLabors.ImageSharp.PointF

// Size/SizeF
System.Drawing.Size → SixLabors.ImageSharp.Size
System.Drawing.SizeF → SixLabors.ImageSharp.SizeF

// Rectangle/RectangleF
System.Drawing.Rectangle → SixLabors.ImageSharp.Rectangle
System.Drawing.RectangleF → SixLabors.ImageSharp.RectangleF

// Graphics
System.Drawing.Graphics → Image.Mutate() or DrawingOptions
```

### Common Operations

#### Creating a Bitmap
```csharp
// OLD
var bitmap = new Bitmap(width, height);

// NEW
var image = new Image<Rgba32>(width, height);
```

#### Loading from File
```csharp
// OLD
var bitmap = new Bitmap(filePath);

// NEW
var image = Image.Load<Rgba32>(filePath);
```

#### Getting/Setting Pixels
```csharp
// OLD
Color color = bitmap.GetPixel(x, y);
bitmap.SetPixel(x, y, color);

// NEW
Rgba32 color = image[x, y];
image[x, y] = color;
```

#### Drawing Operations
```csharp
// OLD
using (var g = Graphics.FromImage(bitmap))
{
    g.DrawLine(pen, x1, y1, x2, y2);
    g.FillRectangle(brush, rect);
}

// NEW
image.Mutate(ctx =>
{
    ctx.DrawLines(color, thickness, new PointF[] { new(x1, y1), new(x2, y2) });
    ctx.Fill(color, rect);
});
```

#### Saving
```csharp
// OLD
bitmap.Save(filePath, ImageFormat.Png);

// NEW
image.SaveAsPng(filePath);
```

## For TiNRS-Tiler

Since we're using SkiaSharp in the UI layer, we might want to keep System.Drawing.Bitmap in the library for now and convert at the boundary. However, for true cross-platform support, ImageSharp is better.

### Alternative: Keep System.Drawing.Common
.NET 6+ includes System.Drawing.Common which works on non-Windows platforms with libgdiplus. This might be easier for initial porting:

```xml
<PackageReference Include="System.Drawing.Common" Version="9.0.0" />
```

However, Microsoft recommends migrating away from System.Drawing.Common for cross-platform scenarios.

## Decision

For this migration, we'll use **System.Drawing.Common** initially to get things compiling, then gradually migrate to ImageSharp/SkiaSharp where needed.
