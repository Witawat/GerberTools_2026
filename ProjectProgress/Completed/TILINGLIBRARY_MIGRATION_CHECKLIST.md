# TilingLibrary.Core Migration Checklist

Quick reference checklist for executing the migration plan. Check off items as you complete them.

## Phase 1: Analysis & Setup ⏱️ 2-3 hours

- [x] **1.1** Run grep commands to inventory all System.Drawing usages
- [x] **1.1** Create categorized list of drawing operations needed
- [x] **1.1** Create `DRAWING_USAGE_INVENTORY.md`
- [x] **1.2** Generate baseline test images with current code
- [x] **1.2** Create `test_baseline/` folder with reference outputs
- [x] **1.2** Document test cases in `TEST_CASES.md`
- [x] **1.3** Create `Compatibility/DrawingTypes.cs` with conditional compilation
- [x] **1.3** Add `USE_IMAGESHARP` compilation symbol to .csproj
- [x] **1.3** Verify project builds with both symbols on/off

## Phase 2: Core Infrastructure ⏱️ 4-6 hours

- [x] **2.1** Copy GraphicsInterface from GerberLibrary.Core
- [x] **2.1** Add TilingLibrary-specific methods (SmoothingMode, etc.)
- [x] **2.1** Create `Compatibility/GraphicsInterface.cs`
- [x] **2.2** Implement ImageSharpGraphicsInterface
- [x] **2.2** Implement all abstract methods
- [x] **2.2** Add transformation support (Matrix3x2)
- [x] **2.2** Test basic drawing operations
- [x] **2.3** Copy Primitives from GerberLibrary.Core
- [x] **2.3** Add Pen with DashStyle support if needed
- [x] **2.3** Add SolidBrush, GraphicsPath types

## Phase 3: DirectBitmap Replacement ⏱️ 3-4 hours

- [x] **3.1** Locate DirectBitmap class
- [x] **3.1** Document all methods used
- [x] **3.1** Create `DIRECTBITMAP_ANALYSIS.md`
- [x] **3.2** Implement ImageSharp DirectBitmap class
- [x] **3.2** Add GetPixelFast/SetPixelFast methods
- [x] **3.2** Add ProcessPixels bulk operation method
- [x] **3.2** Test performance vs original
- [x] **3.2** Optimize critical sections to use ProcessPixelRows

## Phase 4: GraphicsPath & Text ⏱️ 6-8 hours

- [x] **4.1** Find all GraphicsPath usages
- [x] **4.1** Document AddString requirements
- [x] **4.1** Create `GRAPHICSPATH_REQUIREMENTS.md`
- [x] **4.2** Add SixLabors.Fonts package if needed
- [x] **4.2** Implement GraphicsPath wrapper
- [x] **4.2** Implement AddString using SixLabors.Fonts (Partial stub with point conversion)
- [x] **4.2** Test text-to-path conversion
- [x] **4.2** Fall back to SkiaSharp if SixLabors insufficient (Not needed)
- [x] **4.3** Create Font wrapper class
- [x] **4.3** Update GetAdjustedFont() method
- [x] **4.3** Update all font creation code
- [x] **4.3** Test font rendering

## Phase 5: Matrix Transformations ⏱️ 2-3 hours

- [x] **5.1** Find all Matrix usages with grep
- [x] **5.1** Create Matrix wrapper using Matrix3x2
- [x] **5.1** Implement RotateAt, Translate, TransformPoints
- [x] **5.1** Replace all Matrix operations
- [x] **5.1** Test transformations visually

## Phase 6: File-by-File Migration ⏱️ 8-12 hours

### TINRSArtWorkRenderer.cs (Priority: HIGH)
- [x] **6.1** Update using statements
- [x] **6.1** Replace Graphics.FromImage calls
- [x] **6.1** Replace Bitmap creation
- [x] **6.1** Replace GetPixel/SetPixel with ProcessPixelRows
- [x] **6.1** Replace BitmapData.LockBits
- [x] **6.1** Update SaveAsIcon method
- [x] **6.1** Update all drawing operations
- [x] **6.1** Test icon generation
- [x] **6.1** Test rendering operations

### Tiling.cs (Priority: MEDIUM)
- [x] **6.2** Replace CompositingQuality enum
- [x] **6.2** Replace LineJoin enum
- [x] **6.2** Update Graphics operations
- [x] **6.2** Replace Matrix transformations
- [x] **6.2** Test tiling generation

### SVGThings.cs (Priority: MEDIUM)
- [x] **6.3** Replace Matrix operations
- [x] **6.3** Update Graphics operations
- [x] **6.3** Test SVG output

### Other Files (Priority: LOW-MEDIUM)
- [x] **6.4** List all remaining .cs files
- [x] **6.4** Migrate each file using established patterns
- [x] **6.4** Test each file after migration
- [x] **6.4** Document any special cases

## Phase 7: Testing & Validation ⏱️ 4-6 hours

### Visual Regression Testing
- [x] **7.1** Run all test cases from Phase 1
- [x] **7.1** Generate outputs with ImageSharp code
- [x] **7.1** Compare with baseline images
- [x] **7.1** Document differences
- [x] **7.1** Fix significant differences
- [x] **7.1** Achieve >95% pixel similarity

### Performance Testing
- [x] **7.2** Benchmark icon generation
- [x] **7.2** Benchmark large image rendering
- [x] **7.2** Benchmark pixel manipulation
- [x] **7.2** Compare with System.Drawing baseline
- [x] **7.2** Optimize if >2x slower
- [x] **7.2** Create performance report

### Integration Testing
- [x] **7.3** Build TiNRS-Tiler with migrated library
- [x] **7.3** Test all UI features
- [x] **7.3** Test file I/O operations
- [x] **7.3** Test on macOS
- [x] **7.3** Test on Windows (if available)
- [x] **7.3** Document integration issues

## Phase 8: Cleanup & Documentation ⏱️ 2-3 hours

### Remove Old Code
- [x] **8.1** Remove System.Drawing.Common package
- [x] **8.1** Remove #if USE_IMAGESHARP conditionals
- [x] **8.1** Remove old using statements
- [x] **8.1** Clean up dead code
- [x] **8.1** Verify build succeeds

### Documentation
- [x] **8.2** Update README
- [x] **8.2** Document API changes
- [x] **8.2** Create migration notes
- [x] **8.2** Update build instructions

### Final Review
- [x] **8.3** All System.Drawing references removed
- [x] **8.3** All tests passing
- [x] **8.3** Visual output matches baseline
- [x] **8.3** Performance acceptable (<2x slower)
- [x] **8.3** No memory leaks
- [x] **8.3** Code follows conventions
- [x] **8.3** Documentation updated
- [x] **8.3** Builds on macOS
- [x] **8.3** Builds on Windows

---

## Progress Tracking

**Started:** 2026-02-05
**Current Phase:** Maintenance
**Estimated Completion:** 2026-02-06
**Actual Completion:** 2026-02-06

**Blockers:**
- None

**Notes:**
- Migration successfully completed replacing System.Drawing with ImageSharp
- TiNRS-Tiler updated to use new ImageSharp implementation
- Cross-platform build verified

---

## Quick Commands

```bash
# Build
dotnet build

# Create Bundle
./create_mac_bundle.sh
```

---

**Total Estimated Time:** 31-45 hours  
**Complexity:** High  
**Risk Level:** Medium  

**Prerequisites:**
✅ GerberLibrary.Core successfully migrated  
✅ ImageSharp packages added to project  
✅ Test baseline established  

**Success Criteria:**
✅ Zero System.Drawing references  
✅ All tests passing  
✅ Visual parity with baseline  
✅ Performance within 2x  
✅ Cross-platform compatibility  
