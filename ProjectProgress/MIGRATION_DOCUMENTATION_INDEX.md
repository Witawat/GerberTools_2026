# System.Drawing to ImageSharp Migration - Complete Documentation

## Overview

This folder contains complete documentation for migrating the GerberTools project from System.Drawing to SixLabors.ImageSharp for cross-platform compatibility.

## Migration Status

### ✅ Completed: GerberLibrary.Core
- **Status:** Fully migrated, builds with 0 errors
- **Build:** ✅ Success (583 warnings, non-critical)
- **Platform:** Cross-platform (Windows, macOS, Linux)
- **Reference:** See `MIGRATION_COMPLETE.md`

### ⚠️ In Progress: TilingLibrary.Core
- **Status:** Requires significant refactoring
- **Estimated Effort:** 31-45 hours
- **Complexity:** High
- **Reference:** See migration documents below

## Documentation Files

### 1. **MIGRATION_COMPLETE.md**
✅ **Completed migration summary for GerberLibrary.Core**

**Contents:**
- Summary of successful migration
- Key changes made
- Type replacements
- Method adaptations
- Files modified
- Benefits achieved
- Testing recommendations

**Use this as:** Reference for completed work and patterns that worked

---

### 2. **TILINGLIBRARY_MIGRATION_PLAN.md**
📋 **Comprehensive step-by-step migration plan**

**Contents:**
- 8 phases with detailed tasks
- Estimated time for each phase (31-45 hours total)
- Code examples for each pattern
- Troubleshooting guide
- Success criteria
- References

**Use this as:** Primary execution guide for another AI or developer

**Phases:**
1. Analysis & Setup (2-3 hours)
2. Core Infrastructure (4-6 hours)
3. DirectBitmap Replacement (3-4 hours)
4. GraphicsPath & Text Rendering (6-8 hours)
5. Matrix Transformations (2-3 hours)
6. File-by-File Migration (8-12 hours)
7. Testing & Validation (4-6 hours)
8. Cleanup & Documentation (2-3 hours)

---

### 3. **TILINGLIBRARY_MIGRATION_CHECKLIST.md**
☑️ **Quick reference checklist with checkboxes**

**Contents:**
- Checkbox for every task in the plan
- Progress tracking section
- Quick commands reference
- Time estimates per phase
- Prerequisites and success criteria

**Use this as:** Daily tracking tool during migration

---

### 4. **IMAGESHARP_CODE_PATTERNS.md**
💡 **Code pattern reference guide**

**Contents:**
- Side-by-side System.Drawing vs ImageSharp examples
- Common operations (colors, images, drawing, text, etc.)
- Performance patterns
- Common gotchas
- Migration checklist per file

**Use this as:** Quick reference while coding

**Sections:**
- Using Statements
- Color Operations
- Image Creation & Loading
- Drawing Operations
- Pixel Access (slow vs fast)
- Transformations
- Text & Fonts
- Image Saving
- Advanced Operations

---

### 5. **TILINGLIBRARY_MIGRATION_STATUS.md**
📊 **Current status and challenges**

**Contents:**
- Key challenges identified
- Files requiring migration
- Recommended approaches (3 options)
- Immediate next steps
- Estimated effort breakdown
- Current build status

**Use this as:** Context for decision-making

---

## Quick Start Guide for AI Executor

### Step 1: Read the Context
1. Read `MIGRATION_COMPLETE.md` to understand what was already done
2. Read `TILINGLIBRARY_MIGRATION_STATUS.md` to understand the challenges
3. Review `GerberLibrary.Core/` code as reference implementation

### Step 2: Prepare
1. Open `TILINGLIBRARY_MIGRATION_PLAN.md` for detailed instructions
2. Open `TILINGLIBRARY_MIGRATION_CHECKLIST.md` for tracking
3. Keep `IMAGESHARP_CODE_PATTERNS.md` handy for reference

### Step 3: Execute
1. Follow the plan phase by phase
2. Check off items in the checklist as you complete them
3. Refer to code patterns document when stuck
4. Test frequently (after each phase)
5. Document any deviations or issues

### Step 4: Validate
1. Run all tests from Phase 7
2. Compare visual output with baseline
3. Check performance metrics
4. Verify cross-platform builds

## Key Success Factors

### ✅ Do's
- ✅ Work incrementally (phase by phase)
- ✅ Test after each major change
- ✅ Use GerberLibrary.Core as reference
- ✅ Use ProcessPixelRows for bulk pixel operations
- ✅ Keep baseline images for comparison
- ✅ Document any issues or deviations
- ✅ Commit after each completed task

### ❌ Don'ts
- ❌ Don't delete System.Drawing code until ImageSharp works
- ❌ Don't skip testing phases
- ❌ Don't use individual pixel access in loops (slow)
- ❌ Don't assume API equivalence (check patterns doc)
- ❌ Don't mix ARGB and RGBA parameter orders
- ❌ Don't forget to dispose Image instances

## Common Patterns

### Pattern 1: Simple Drawing Operation
```csharp
// Before (System.Drawing)
using (var bitmap = new Bitmap(100, 100))
using (var g = Graphics.FromImage(bitmap))
{
    g.Clear(Color.White);
    g.DrawLine(new Pen(Color.Red, 2), 0, 0, 100, 100);
    bitmap.Save("output.png", ImageFormat.Png);
}

// After (ImageSharp)
using (var image = new Image<Rgba32>(100, 100))
{
    image.Mutate(ctx =>
    {
        ctx.Clear(Color.White);
        ctx.DrawLine(new SolidPen(Color.Red, 2), 
                     new PointF(0, 0), new PointF(100, 100));
    });
    image.SaveAsPng("output.png");
}
```

### Pattern 2: Fast Pixel Processing
```csharp
// Before (System.Drawing with LockBits)
BitmapData data = bitmap.LockBits(...);
unsafe
{
    byte* ptr = (byte*)data.Scan0;
    // Process pixels
}
bitmap.UnlockBits(data);

// After (ImageSharp)
image.ProcessPixelRows(accessor =>
{
    for (int y = 0; y < accessor.Height; y++)
    {
        Span<Rgba32> row = accessor.GetRowSpan(y);
        // Process pixels
    }
});
```

### Pattern 3: Using GraphicsInterface
```csharp
// Custom abstraction (recommended)
var g = new ImageSharpGraphicsInterface(image);
g.Clear(Color.White);
g.DrawLine(pen, p1, p2);
g.FillRectangle(brush, x, y, w, h);
// No dispose needed - GraphicsInterface doesn't own the image
```

## Troubleshooting

### Issue: Build errors about missing types
**Solution:** Check using statements and type aliases

### Issue: Colors look wrong
**Solution:** Check ARGB vs RGBA parameter order in Color.FromArgb/FromRgba

### Issue: Performance is slow
**Solution:** Use ProcessPixelRows instead of individual pixel access

### Issue: Visual output doesn't match
**Solution:** 
- Check anti-aliasing settings
- Verify color conversions
- Compare rendering options
- Check font rendering settings

### Issue: Text rendering issues
**Solution:**
- Ensure SixLabors.Fonts is installed
- Check font availability with SystemFonts
- Verify text positioning (baseline differences)

## Resources

### Documentation
- [SixLabors.ImageSharp Docs](https://docs.sixlabors.com/api/ImageSharp/)
- [SixLabors.ImageSharp.Drawing Docs](https://docs.sixlabors.com/api/ImageSharp.Drawing/)
- [SixLabors.Fonts Docs](https://docs.sixlabors.com/api/Fonts/)

### Reference Implementation
- `GerberLibrary.Core/Core/GraphicsInterface.cs` - Graphics abstraction
- `GerberLibrary.Core/Core/Primitives/GraphicsPrimitives.cs` - Primitive types
- `GerberLibrary.Core/Core/ImageCreator.cs` - Image operations
- `GerberLibrary.Core/Artwork Related/SVGWriter.cs` - SVG rendering

### Community
- [ImageSharp GitHub](https://github.com/SixLabors/ImageSharp)
- [ImageSharp Discussions](https://github.com/SixLabors/ImageSharp/discussions)

## Timeline

### Completed
- ✅ **2026-02-05:** GerberLibrary.Core migration complete
- ✅ **2026-02-06:** Migration documentation created

### Planned
- 📋 **TBD:** TilingLibrary.Core migration start
- 📋 **TBD:** TilingLibrary.Core migration complete
- 📋 **TBD:** Full application testing on macOS
- 📋 **TBD:** Production release

## Estimated Effort Summary

| Component | Status | Effort | Complexity |
|-----------|--------|--------|------------|
| GerberLibrary.Core | ✅ Complete | ~20 hours | Medium |
| TilingLibrary.Core | ⚠️ Planned | 31-45 hours | High |
| Testing & Validation | ⚠️ Pending | 8-12 hours | Medium |
| **TOTAL** | | **59-77 hours** | |

## Success Criteria

### GerberLibrary.Core ✅
- [x] Zero System.Drawing references
- [x] Builds without errors
- [x] 583 warnings (non-critical)
- [x] Cross-platform compatible

### TilingLibrary.Core (Pending)
- [ ] Zero System.Drawing references
- [ ] Builds without errors
- [ ] All tests passing
- [ ] Visual output matches baseline (>95%)
- [ ] Performance within 2x of original
- [ ] Works on macOS and Windows
- [ ] No memory leaks
- [ ] Documentation updated

## Next Steps

1. **Review all documentation** - Understand the scope and approach
2. **Set up environment** - Ensure all tools and dependencies are ready
3. **Create baseline** - Generate reference images before migration
4. **Start Phase 1** - Begin analysis and setup
5. **Execute systematically** - Follow the plan phase by phase
6. **Test continuously** - Validate after each phase
7. **Document progress** - Update checklist and notes

## Contact & Support

For questions or issues during migration:
1. Refer to `IMAGESHARP_CODE_PATTERNS.md` for code examples
2. Check `TILINGLIBRARY_MIGRATION_PLAN.md` troubleshooting section
3. Review GerberLibrary.Core implementation for working examples
4. Consult SixLabors documentation and community

---

**Documentation Version:** 1.0  
**Created:** 2026-02-06  
**Last Updated:** 2026-02-06  
**Status:** Complete and ready for execution

**Good luck with the migration! 🚀**
