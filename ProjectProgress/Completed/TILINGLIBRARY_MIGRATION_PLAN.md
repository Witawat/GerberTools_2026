# TilingLibrary.Core to ImageSharp Migration Plan

## Executive Summary

This document provides a step-by-step plan to migrate TilingLibrary.Core from System.Drawing to SixLabors.ImageSharp. The migration is estimated at 20-40 hours and should be completed in phases to maintain testability.

**Prerequisites:**
- GerberLibrary.Core has been successfully migrated (use as reference)
- ImageSharp packages already added to TilingLibrary.Core.csproj
- Familiarity with both System.Drawing and ImageSharp APIs

---

## Phase 1: Analysis & Setup (2-3 hours)

### Task 1.1: Inventory System.Drawing Usage
**Goal:** Create a complete list of all System.Drawing dependencies

**Steps:**
1. Run grep to find all System.Drawing usages:
   ```bash
   cd TilingLibrary.Core
   grep -r "using System.Drawing" . --include="*.cs" > drawing_usages.txt
   grep -r "System.Drawing\." . --include="*.cs" >> drawing_usages.txt
   ```

2. Categorize usages by type:
   - Graphics operations (DrawLine, FillRectangle, etc.)
   - Bitmap operations (GetPixel, SetPixel, LockBits)
   - Font/Text operations (MeasureString, DrawString, GraphicsPath)
   - Transformations (Matrix, RotateTransform, etc.)
   - Image I/O (Save, FromFile, etc.)
   - Advanced features (CompositingMode, SmoothingMode, etc.)

3. Create a spreadsheet or markdown table with:
   - File name
   - Line number
   - System.Drawing type/method used
   - Complexity (Low/Medium/High)
   - ImageSharp equivalent (if known)

**Deliverable:** `DRAWING_USAGE_INVENTORY.md`

### Task 1.2: Create Test Baseline
**Goal:** Establish visual regression tests before migration

**Steps:**
1. Identify key rendering methods that produce output
2. Generate sample outputs with current System.Drawing code
3. Save outputs as reference images in `test_baseline/` folder
4. Document the exact parameters used for each test case

**Deliverable:** 
- `test_baseline/` folder with reference images
- `TEST_CASES.md` documenting how to reproduce each output

### Task 1.3: Set Up Type Aliases
**Goal:** Prepare the codebase for gradual migration

**Steps:**
1. Create `TilingLibrary.Core/Compatibility/DrawingTypes.cs`:
   ```csharp
   #if USE_IMAGESHARP
   using Bitmap = SixLabors.ImageSharp.Image<SixLabors.ImageSharp.PixelFormats.Rgba32>;
   using Graphics = TilingLibrary.Core.Compatibility.GraphicsInterface;
   using Color = SixLabors.ImageSharp.Color;
   using PointF = SixLabors.ImageSharp.PointF;
   using SizeF = SixLabors.ImageSharp.SizeF;
   using RectangleF = SixLabors.ImageSharp.RectangleF;
   using Font = SixLabors.Fonts.Font;
   using FontFamily = SixLabors.Fonts.FontFamily;
   using FontStyle = SixLabors.Fonts.FontStyle;
   #else
   using Bitmap = System.Drawing.Bitmap;
   using Graphics = System.Drawing.Graphics;
   using Color = System.Drawing.Color;
   using PointF = System.Drawing.PointF;
   using SizeF = System.Drawing.SizeF;
   using RectangleF = System.Drawing.RectangleF;
   using Font = System.Drawing.Font;
   using FontFamily = System.Drawing.FontFamily;
   using FontStyle = System.Drawing.FontStyle;
   #endif
   ```

2. Add conditional compilation symbol to .csproj:
   ```xml
   <PropertyGroup Condition="'$(UseImageSharp)' == 'true'">
     <DefineConstants>$(DefineConstants);USE_IMAGESHARP</DefineConstants>
   </PropertyGroup>
   ```

**Deliverable:** Conditional compilation infrastructure

---

## Phase 2: Core Infrastructure (4-6 hours)

### Task 2.1: Create GraphicsInterface Abstraction
**Goal:** Abstract drawing operations similar to GerberLibrary.Core

**Steps:**
1. Copy `GerberLibrary.Core/Core/GraphicsInterface.cs` as starting point
2. Adapt to TilingLibrary needs:
   - Add any missing drawing methods used in TilingLibrary
   - Add SmoothingMode, CompositingMode, TextRenderingHint properties
   - Add Matrix transformation support

3. Create `TilingLibrary.Core/Compatibility/GraphicsInterface.cs`:
   ```csharp
   public abstract class GraphicsInterface
   {
       // Core drawing
       public abstract void Clear(Color color);
       public abstract void DrawLine(Pen pen, PointF p1, PointF p2);
       public abstract void DrawPolygon(Pen pen, PointF[] points);
       public abstract void FillPolygon(SolidBrush brush, PointF[] points);
       
       // Text rendering
       public abstract SizeF MeasureString(string text, Font font);
       public abstract void DrawString(string text, Font font, SolidBrush brush, PointF location);
       
       // Transformations
       public abstract void RotateTransform(float angle);
       public abstract void TranslateTransform(float dx, float dy);
       public abstract void ScaleTransform(float sx, float sy);
       
       // Advanced properties
       public abstract SmoothingMode SmoothingMode { get; set; }
       public abstract CompositingMode CompositingMode { get; set; }
       public abstract InterpolationMode InterpolationMode { get; set; }
       public abstract TextRenderingHint TextRenderingHint { get; set; }
   }
   ```

**Deliverable:** `GraphicsInterface.cs` with all required methods

### Task 2.2: Implement ImageSharpGraphicsInterface
**Goal:** Concrete ImageSharp implementation

**Steps:**
1. Create `TilingLibrary.Core/Compatibility/ImageSharpGraphicsInterface.cs`
2. Implement all abstract methods from GraphicsInterface
3. Use System.Numerics.Matrix3x2 for transformations
4. Store rendering options (smoothing, compositing) as state
5. Apply options in GetOptions() helper method

**Key Implementation Notes:**
- SmoothingMode → Use Antialias option in DrawingOptions
- CompositingMode → Use GraphicsOptions.BlendPercentage
- TextRenderingHint → Use TextOptions with appropriate settings
- Matrix transformations → Accumulate in Matrix3x2, apply via DrawingOptions.Transform

**Deliverable:** `ImageSharpGraphicsInterface.cs` fully implemented

### Task 2.3: Create Primitives
**Goal:** Define cross-platform primitive types

**Steps:**
1. Copy `GerberLibrary.Core/Core/Primitives/GraphicsPrimitives.cs`
2. Add any TilingLibrary-specific types:
   - Pen (with DashStyle support if needed)
   - SolidBrush
   - GraphicsPath equivalent
   - Matrix wrapper (if needed)

**Deliverable:** `TilingLibrary.Core/Primitives/` folder with all types

---

## Phase 3: DirectBitmap Replacement (3-4 hours)

### Task 3.1: Analyze DirectBitmap Usage
**Goal:** Understand the fast pixel access requirements

**Steps:**
1. Locate DirectBitmap class (likely in TilingLibrary.Core or separate file)
2. Document all methods used:
   - GetPixelFast()
   - SetPixelFast()
   - Constructor
   - Dispose pattern

3. Identify performance-critical sections using DirectBitmap

**Deliverable:** `DIRECTBITMAP_ANALYSIS.md`

### Task 3.2: Implement ImageSharp DirectBitmap
**Goal:** Create ImageSharp equivalent with similar performance

**Steps:**
1. Create `TilingLibrary.Core/Compatibility/DirectBitmap.cs`:
   ```csharp
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
           var pixel = color.ToPixel<Rgba32>();
           _image[x, y] = pixel;
       }
       
       // For bulk operations, use ProcessPixelRows
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
       
       public void Dispose()
       {
           _image?.Dispose();
       }
   }
   ```

2. Optimize performance-critical sections to use ProcessPixelRows instead of GetPixelFast loops

**Deliverable:** `DirectBitmap.cs` with ImageSharp implementation

---

## Phase 4: GraphicsPath & Text Rendering (6-8 hours)

### Task 4.1: Analyze GraphicsPath Usage
**Goal:** Understand text-to-path conversion requirements

**Steps:**
1. Find all GraphicsPath usages in codebase
2. Document what's needed:
   - AddString() for text-to-path conversion
   - Path manipulation
   - Drawing/filling paths

**Deliverable:** `GRAPHICSPATH_REQUIREMENTS.md`

### Task 4.2: Implement Path Abstraction
**Goal:** Create cross-platform path handling

**Options:**

**Option A: Use SixLabors.ImageSharp.Drawing (Simpler)**
```csharp
public class GraphicsPath
{
    private PathBuilder _builder = new PathBuilder();
    
    public void AddString(string text, FontFamily family, int style, 
                         float size, PointF location, StringFormat format)
    {
        // Use SixLabors.Fonts to render text to path
        var font = family.CreateFont(size, (FontStyle)style);
        var glyphs = TextBuilder.GenerateGlyphs(text, location, 
                                                new TextOptions(font));
        foreach (var glyph in glyphs)
        {
            _builder.AddPath(glyph.Path);
        }
    }
    
    public IPath Build() => _builder.Build();
}
```

**Option B: Use SkiaSharp (More Features)**
- If SixLabors.Fonts doesn't provide sufficient text-to-path conversion
- Add SkiaSharp package
- Use SKPath for advanced path operations

**Recommendation:** Try Option A first, fall back to Option B if needed

**Steps:**
1. Add SixLabors.Fonts package if not already present
2. Implement GraphicsPath wrapper
3. Test with actual text rendering from TINRSArtWorkRenderer

**Deliverable:** `GraphicsPath.cs` implementation

### Task 4.3: Font Handling
**Goal:** Replace System.Drawing.Font with SixLabors.Fonts

**Steps:**
1. Create Font wrapper if needed:
   ```csharp
   public class Font
   {
       private SixLabors.Fonts.Font _font;
       
       public Font(string familyName, float size, FontStyle style)
       {
           var family = SystemFonts.Find(familyName) ?? SystemFonts.Families.First();
           _font = family.CreateFont(size, (SixLabors.Fonts.FontStyle)style);
       }
       
       public string Name => _font.Family.Name;
       public float Size => _font.Size;
       public SixLabors.Fonts.Font InnerFont => _font;
   }
   ```

2. Update GetAdjustedFont() method to use ImageSharp font measurement
3. Update all font creation code

**Deliverable:** Font wrapper and updated font handling

---

## Phase 5: Matrix Transformations (2-3 hours)

### Task 5.1: Replace System.Drawing.Drawing2D.Matrix
**Goal:** Use System.Numerics.Matrix3x2 throughout

**Steps:**
1. Find all Matrix usages:
   ```bash
   grep -r "new Matrix()" . --include="*.cs"
   grep -r "Matrix " . --include="*.cs" | grep -v "Matrix3x2"
   ```

2. Create Matrix wrapper if needed:
   ```csharp
   public class Matrix
   {
       private Matrix3x2 _matrix = Matrix3x2.Identity;
       
       public void Reset() => _matrix = Matrix3x2.Identity;
       
       public void RotateAt(float angle, PointF center)
       {
           var radians = angle * (float)(Math.PI / 180.0);
           var translation1 = Matrix3x2.CreateTranslation(-center.X, -center.Y);
           var rotation = Matrix3x2.CreateRotation(radians);
           var translation2 = Matrix3x2.CreateTranslation(center.X, center.Y);
           _matrix = translation1 * rotation * translation2 * _matrix;
       }
       
       public void Translate(float dx, float dy)
       {
           _matrix = Matrix3x2.CreateTranslation(dx, dy) * _matrix;
       }
       
       public void TransformPoints(PointF[] points)
       {
           for (int i = 0; i < points.Length; i++)
           {
               var p = new Vector2(points[i].X, points[i].Y);
               p = Vector2.Transform(p, _matrix);
               points[i] = new PointF(p.X, p.Y);
           }
       }
       
       public Matrix3x2 InnerMatrix => _matrix;
   }
   ```

3. Replace all Matrix operations

**Deliverable:** Matrix wrapper and updated transformation code

---

## Phase 6: File-by-File Migration (8-12 hours)

### Task 6.1: Migrate TINRSArtWorkRenderer.cs
**Priority:** HIGH (900 lines, most complex)

**Steps:**
1. Update using statements (already partially done)
2. Replace Graphics.FromImage() calls:
   ```csharp
   // Old:
   Graphics g = Graphics.FromImage(bitmap);
   
   // New:
   var graphicsInterface = new ImageSharpGraphicsInterface(bitmap);
   ```

3. Replace Bitmap creation:
   ```csharp
   // Old:
   Bitmap b = new Bitmap(w, h, PixelFormat.Format32bppArgb);
   
   // New:
   var b = new Image<Rgba32>(w, h);
   ```

4. Replace GetPixel/SetPixel loops with ProcessPixelRows:
   ```csharp
   // Old:
   for (int i = 0; i < h; i++)
   {
       for (int j = 0; j < w; j++)
       {
           var c = bitmap.GetPixel(j, i);
           // process
           bitmap.SetPixel(j, i, newColor);
       }
   }
   
   // New:
   bitmap.ProcessPixelRows(accessor =>
   {
       for (int y = 0; y < accessor.Height; y++)
       {
           Span<Rgba32> row = accessor.GetRowSpan(y);
           for (int x = 0; x < row.Length; x++)
           {
               var pixel = row[x];
               // process
               row[x] = newPixel;
           }
       }
   });
   ```

5. Replace BitmapData.LockBits:
   ```csharp
   // Old:
   BitmapData data = bitmap.LockBits(...);
   unsafe
   {
       byte* ptr = (byte*)data.Scan0;
       // access pixels
   }
   bitmap.UnlockBits(data);
   
   // New:
   bitmap.ProcessPixelRows(accessor =>
   {
       for (int y = 0; y < accessor.Height; y++)
       {
           Span<Rgba32> row = accessor.GetRowSpan(y);
           // access pixels via span
       }
   });
   ```

6. Update icon generation (SaveAsIcon):
   - Use ImageSharp PNG encoder
   - Implement custom ICO format writer or use library

7. Update all drawing operations to use GraphicsInterface

**Deliverable:** Migrated TINRSArtWorkRenderer.cs

### Task 6.2: Migrate Tiling.cs
**Priority:** MEDIUM

**Steps:**
1. Replace CompositingQuality, LineJoin enums with custom equivalents
2. Update Graphics operations to use GraphicsInterface
3. Replace Matrix transformations

**Deliverable:** Migrated Tiling.cs

### Task 6.3: Migrate SVGThings.cs
**Priority:** MEDIUM

**Steps:**
1. Replace Matrix operations
2. Update any Graphics operations

**Deliverable:** Migrated SVGThings.cs

### Task 6.4: Migrate Remaining Files
**Priority:** LOW-MEDIUM

**Steps:**
1. Go through each .cs file in TilingLibrary.Core
2. Apply same patterns as above
3. Test each file after migration

**Deliverable:** All files migrated

---

## Phase 7: Testing & Validation (4-6 hours)

### Task 7.1: Visual Regression Testing
**Goal:** Ensure output matches baseline

**Steps:**
1. Run all test cases from Phase 1
2. Generate new outputs with ImageSharp code
3. Compare pixel-by-pixel with baseline images
4. Document any differences
5. Adjust code if differences are significant

**Acceptance Criteria:**
- Visual output matches within acceptable tolerance (e.g., 95% pixel similarity)
- No crashes or exceptions
- Performance is acceptable (within 2x of original)

**Deliverable:** Test results report

### Task 7.2: Performance Testing
**Goal:** Ensure performance is acceptable

**Steps:**
1. Benchmark key operations:
   - Icon generation
   - Large image rendering
   - Pixel manipulation loops

2. Compare with System.Drawing baseline
3. Optimize hot paths if needed:
   - Use ProcessPixelRows instead of individual pixel access
   - Cache font measurements
   - Reuse Image instances

**Deliverable:** Performance report

### Task 7.3: Integration Testing
**Goal:** Test in actual application context

**Steps:**
1. Build TiNRS-Tiler with migrated TilingLibrary.Core
2. Test all UI features
3. Test file I/O operations
4. Test on both macOS and Windows (if available)

**Deliverable:** Integration test results

---

## Phase 8: Cleanup & Documentation (2-3 hours)

### Task 8.1: Remove System.Drawing.Common
**Goal:** Complete the migration

**Steps:**
1. Remove System.Drawing.Common package reference from .csproj
2. Remove all #if USE_IMAGESHARP conditionals
3. Remove old System.Drawing using statements
4. Clean up any dead code

**Deliverable:** Clean codebase

### Task 8.2: Update Documentation
**Goal:** Document the migration

**Steps:**
1. Update README with ImageSharp dependency info
2. Document any API changes
3. Create migration notes for future reference
4. Update build instructions

**Deliverable:** Updated documentation

### Task 8.3: Code Review Checklist
**Goal:** Final quality check

**Checklist:**
- [ ] All System.Drawing references removed
- [ ] All tests passing
- [ ] Visual output matches baseline
- [ ] Performance acceptable
- [ ] No memory leaks (test with long-running operations)
- [ ] Code follows project conventions
- [ ] Documentation updated
- [ ] Build succeeds on macOS
- [ ] Build succeeds on Windows (if applicable)

**Deliverable:** Completed checklist

---

## Common Patterns & Solutions

### Pattern 1: Color Conversion
```csharp
// System.Drawing → ImageSharp
Color.FromArgb(r, g, b) → Color.FromRgb((byte)r, (byte)g, (byte)b)
Color.FromArgb(a, r, g, b) → Color.FromRgba((byte)r, (byte)g, (byte)b, (byte)a)
color.R → color.ToPixel<Rgba32>().R
```

### Pattern 2: Image Creation
```csharp
// System.Drawing → ImageSharp
new Bitmap(w, h) → new Image<Rgba32>(w, h)
new Bitmap(w, h, PixelFormat.Format32bppArgb) → new Image<Rgba32>(w, h)
Bitmap.FromFile(path) → Image.Load<Rgba32>(path)
bitmap.Save(path, ImageFormat.Png) → bitmap.SaveAsPng(path)
```

### Pattern 3: Drawing Context
```csharp
// System.Drawing → ImageSharp
Graphics g = Graphics.FromImage(bitmap);
g.DrawLine(...);

// Becomes:
var g = new ImageSharpGraphicsInterface(bitmap);
g.DrawLine(...);

// Or for single operations:
bitmap.Mutate(ctx => ctx.DrawLine(...));
```

### Pattern 4: Pixel Access
```csharp
// System.Drawing → ImageSharp (slow)
var color = bitmap.GetPixel(x, y);

// ImageSharp (fast)
bitmap.ProcessPixelRows(accessor =>
{
    var pixel = accessor.GetRowSpan(y)[x];
});
```

---

## Troubleshooting Guide

### Issue: "Type 'X' could not be found"
**Solution:** Add appropriate using statement or type alias

### Issue: "Cannot convert from 'Color' to 'Color'"
**Solution:** Namespace conflict - use fully qualified names or adjust using statements

### Issue: "Method 'X' does not exist"
**Solution:** Check ImageSharp API documentation for equivalent method

### Issue: Performance degradation
**Solution:** 
- Use ProcessPixelRows for bulk operations
- Cache frequently used objects (fonts, brushes, pens)
- Profile to find hot paths

### Issue: Visual differences in output
**Solution:**
- Check anti-aliasing settings
- Verify color space conversions
- Check interpolation modes
- Compare rendering options

---

## Estimated Timeline

| Phase | Tasks | Estimated Hours |
|-------|-------|----------------|
| 1. Analysis & Setup | 1.1-1.3 | 2-3 |
| 2. Core Infrastructure | 2.1-2.3 | 4-6 |
| 3. DirectBitmap | 3.1-3.2 | 3-4 |
| 4. GraphicsPath & Text | 4.1-4.3 | 6-8 |
| 5. Matrix Transformations | 5.1 | 2-3 |
| 6. File-by-File Migration | 6.1-6.4 | 8-12 |
| 7. Testing & Validation | 7.1-7.3 | 4-6 |
| 8. Cleanup & Documentation | 8.1-8.3 | 2-3 |
| **TOTAL** | | **31-45 hours** |

---

## Success Criteria

✅ All System.Drawing references removed  
✅ Project builds without errors  
✅ All tests pass  
✅ Visual output matches baseline (>95% similarity)  
✅ Performance within 2x of original  
✅ Works on macOS and Windows  
✅ No memory leaks  
✅ Documentation updated  

---

## References

- [SixLabors.ImageSharp Documentation](https://docs.sixlabors.com/api/ImageSharp/)
- [SixLabors.ImageSharp.Drawing Documentation](https://docs.sixlabors.com/api/ImageSharp.Drawing/)
- [SixLabors.Fonts Documentation](https://docs.sixlabors.com/api/Fonts/)
- [GerberLibrary.Core Migration](../MIGRATION_COMPLETE.md) - Completed reference implementation
- [System.Drawing to ImageSharp Migration Guide](https://github.com/SixLabors/ImageSharp/discussions/1470)

---

## Notes for AI Executor

1. **Work incrementally** - Complete each phase before moving to the next
2. **Test frequently** - Run builds after each major change
3. **Keep baseline** - Don't delete System.Drawing code until ImageSharp version works
4. **Use GerberLibrary.Core as reference** - It's already successfully migrated
5. **Ask for clarification** - If requirements are unclear, document assumptions
6. **Track progress** - Update this document with completion status
7. **Save intermediate work** - Commit after each completed task
8. **Performance matters** - ImageSharp can be slower; optimize hot paths

---

**Document Version:** 1.0  
**Created:** 2026-02-06  
**Last Updated:** 2026-02-06  
**Status:** Ready for execution
