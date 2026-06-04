# Drawing Usage Inventory - TilingLibrary.Core

This document inventories all `System.Drawing` and related GDI+ usages in `TilingLibrary.Core` to guide the migration to `SixLabors.ImageSharp`.

## Categorized Usages

### 1. Basic Types (Structs)
| Type | Files | Usage Description |
|------|-------|-------------------|
| `Color` | `Tiling.cs`, `TINRSArtWorkRenderer.cs`, `DelaunayBuilder.cs`, `SVGThings.cs` | Background colors, fill colors, pen colors. Extensive use. |
| `Point` / `PointF` | `DelaunayBuilder.cs`, `Tiling.cs`, `TINRSArtWorkRenderer.cs` | Vertex coordinates, drawing positions. |
| `Rectangle` / `RectangleF` | `Tiling.cs`, `TINRSArtWorkRenderer.cs` | Bounds checking, drawing areas. |
| `Size` | `TINRSArtWorkRenderer.cs` | Image dimensions. |

### 2. Image Containers
| Type | Files | Usage Description |
|------|-------|-------------------|
| `Bitmap` | `TINRSArtWorkRenderer.cs`, `DirectBitmap.cs` | Buffer for rendering, icon generation. |
| `DirectBitmap` | `TINRSArtWorkRenderer.cs`, `DirectBitmap.cs` | Custom wrapper for fast pixel access (uses `BitmapData`). |
| `ImageFormat` | `TINRSArtWorkRenderer.cs` | Saving as PNG. |
| `PixelFormat` | `TINRSArtWorkRenderer.cs` | Specifying `Format32bppArgb`. |

### 3. Drawing Contexts & State
| Type | Files | Usage Description |
|------|-------|-------------------|
| `Graphics` | `TINRSArtWorkRenderer.cs`, `Announcer.cs`, `Tiling.cs` | Main drawing context. |
| `SmoothingMode` | `Announcer.cs`, `Tiling.cs`, `TINRSArtWorkRenderer.cs` | Set to `AntiAlias`. |
| `CompositingQuality` | `Tiling.cs`, `TINRSArtWorkRenderer.cs` | Set to `HighQuality`. |
| `InterpolationMode` | `Announcer.cs`, `TINRSArtWorkRenderer.cs` | Set to `High`. |
| `TextRenderingHint` | `Announcer.cs`, `TINRSArtWorkRenderer.cs` | Set to `AntiAlias`. |

### 4. Brushes and Pens
| Type | Files | Usage Description |
|------|-------|-------------------|
| `Pen` | `DelaunayBuilder.cs`, `Tiling.cs`, `TINRSArtWorkRenderer.cs` | Outlining shapes. Uses `Width`, `Color`, `EndCap`, `LineJoin`. |
| `Brush` / `SolidBrush` | `Tiling.cs`, `TINRSArtWorkRenderer.cs` | Filling shapes. |
| `LineCap` | `Tiling.cs` | Set to `Round`. |
| `LineJoin` | `Tiling.cs` | Set to `Round`. |

### 5. Advanced Geometry
| Type | Files | Usage Description |
|------|-------|-------------------|
| `Matrix` | `DelaunayBuilder.cs`, `SVGThings.cs`, `Tiling.cs` | Transformations (Translate, Rotate, TransformPoints). |
| `GraphicsPath` | `Tiling.cs`, `TINRSArtWorkRenderer.cs` | Complex shape construction. |

### 6. Utilities
| Type | Files | Usage Description |
|------|-------|-------------------|
| `ColorTranslator` | `SVGThings.cs` | `FromHtml` conversion. |

## File-Specific Inventory

### TINRSArtWorkRenderer.cs
- `Bitmap` creation and disposal.
- `Graphics.FromImage`.
- Setting rendering hints (`InterpolationMode`, `SmoothingMode`, etc.).
- `DirectBitmap` usage for pixel manipulation.
- `Save` as PNG.
- `Graphics.DrawImage`, `FillPolygon`, `FillPath`.

### Tiling.cs
- `Color` properties.
- `Graphics.FillPolygon`, `DrawPolygon`.
- `GraphicsPath` for complex tiling patterns.
- `SmoothingMode`, `CompositingQuality`.
- `Pen` properties (`LineJoin`, `EndCap`).

### SVGThings.cs
- `Matrix` transformations for SVG elements.
- `ColorTranslator.FromHtml`.

### DelaunayBuilder.cs
- `Matrix` for transforming point sets.
- `PointF` arrays.
- `Pen` for rendering results.

### DirectBitmap.cs (CRITICAL)
- Uses `System.Drawing.Common`'s `Bitmap` and `BitmapData`.
- Uses `LockBits` and `UnlockBits`.
- Needs complete rewrite using ImageSharp's `ProcessPixelRows` or similar.
