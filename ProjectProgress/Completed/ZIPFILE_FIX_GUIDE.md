# Quick Fix Guide: ZipFile API Migration

## Overview
This guide provides step-by-step instructions to fix the remaining 29 compilation errors related to ZipFile API differences between DotNetZip (Ionic.Zip) and SharpZipLib.

---

## Files to Fix

1. **GerberPanel.cs** - Lines 213, 1840, 2389
2. **Gerber.cs** - Line 1333
3. **ImageCreator.cs** - Lines 288, 290, 292

---

## API Mapping Reference

### Reading ZIP Files

**OLD (DotNetZip):**
```csharp
using (Ionic.Zip.ZipFile zip1 = Ionic.Zip.ZipFile.Read(path))
{
    foreach (ZipEntry e in zip1)
    {
        if (e.IsDirectory == false)
        {
            MemoryStream MS = new MemoryStream();
            e.Extract(MS);
            MS.Seek(0, SeekOrigin.Begin);
            Files[e.FileName] = MS;
        }
    }
}
```

**NEW (SharpZipLib):**
```csharp
using (ICSharpCode.SharpZipLib.Zip.ZipFile zip1 = new ICSharpCode.SharpZipLib.Zip.ZipFile(path))
{
    foreach (ICSharpCode.SharpZipLib.Zip.ZipEntry e in zip1)
    {
        if (!e.IsDirectory)
        {
            MemoryStream MS = new MemoryStream();
            using (var stream = zip1.GetInputStream(e))
            {
                stream.CopyTo(MS);
            }
            MS.Seek(0, SeekOrigin.Begin);
            Files[e.Name] = MS;
        }
    }
}
```

### Writing ZIP Files

**OLD (DotNetZip):**
```csharp
using (Ionic.Zip.ZipFile zip2 = new Ionic.Zip.ZipFile())
{
    zip2.AddFile(filePath, ".");
    zip2.Save(outputPath);
}
```

**NEW (SharpZipLib):**
```csharp
using (FileStream fsOut = File.Create(outputPath))
using (ICSharpCode.SharpZipLib.Zip.ZipOutputStream zipStream = new ICSharpCode.SharpZipLib.Zip.ZipOutputStream(fsOut))
{
    zipStream.SetLevel(9); // 0-9, 9 being the highest level of compression
    
    byte[] buffer = new byte[4096];
    
    ICSharpCode.SharpZipLib.Zip.ZipEntry entry = new ICSharpCode.SharpZipLib.Zip.ZipEntry(Path.GetFileName(filePath));
    entry.DateTime = DateTime.Now;
    
    zipStream.PutNextEntry(entry);
    
    using (FileStream fs = File.OpenRead(filePath))
    {
        int sourceBytes;
        do
        {
            sourceBytes = fs.Read(buffer, 0, buffer.Length);
            zipStream.Write(buffer, 0, sourceBytes);
        } while (sourceBytes > 0);
    }
    
    zipStream.CloseEntry();
    zipStream.Finish();
    zipStream.Close();
}
```

---

## Specific Fixes

### 1. GerberPanel.cs - Line 213 (Reading ZIP)

**Location:** `AddGerberZip` method

**Current Code:**
```csharp
using (Ionic.Zip.ZipFile zip1 = Ionic.Zip.ZipFile.Read(path))
{
    foreach (ZipEntry e in zip1)
    {
        MemoryStream MS = new MemoryStream();
        if (e.IsDirectory == false)
        {
            e.Extract(MS);
            MS.Seek(0, SeekOrigin.Begin);
            Files[e.FileName] = MS;
        }
    }
}
```

**Fixed Code:**
```csharp
using (ICSharpCode.SharpZipLib.Zip.ZipFile zip1 = new ICSharpCode.SharpZipLib.Zip.ZipFile(path))
{
    foreach (ICSharpCode.SharpZipLib.Zip.ZipEntry e in zip1)
    {
        MemoryStream MS = new MemoryStream();
        if (!e.IsDirectory)
        {
            using (var stream = zip1.GetInputStream(e))
            {
                stream.CopyTo(MS);
            }
            MS.Seek(0, SeekOrigin.Begin);
            Files[e.Name] = MS;
        }
    }
}
```

---

### 2. GerberPanel.cs - Line 1840 (Writing ZIP)

**Location:** `SaveGerbersToFolder` method

**Current Code:**
```csharp
using (Ionic.Zip.ZipFile zip2 = new Ionic.Zip.ZipFile())
{
    foreach (var a in Files)
    {
        zip2.AddFile(a, ".");
    }
    zip2.Save(TargetZip);
}
```

**Fixed Code:**
```csharp
using (FileStream fsOut = File.Create(TargetZip))
using (ICSharpCode.SharpZipLib.Zip.ZipOutputStream zipStream = new ICSharpCode.SharpZipLib.Zip.ZipOutputStream(fsOut))
{
    zipStream.SetLevel(9);
    byte[] buffer = new byte[4096];
    
    foreach (var filePath in Files)
    {
        ICSharpCode.SharpZipLib.Zip.ZipEntry entry = new ICSharpCode.SharpZipLib.Zip.ZipEntry(Path.GetFileName(filePath));
        entry.DateTime = DateTime.Now;
        
        zipStream.PutNextEntry(entry);
        
        using (FileStream fs = File.OpenRead(filePath))
        {
            int sourceBytes;
            do
            {
                sourceBytes = fs.Read(buffer, 0, buffer.Length);
                zipStream.Write(buffer, 0, sourceBytes);
            } while (sourceBytes > 0);
        }
        
        zipStream.CloseEntry();
    }
    
    zipStream.Finish();
}
```

---

### 3. GerberPanel.cs - Line 2389 (Reading ZIP)

**Same as Fix #1** - Use the reading ZIP pattern

---

### 4. Gerber.cs - Line 1333 (Reading ZIP)

**Same as Fix #1** - Use the reading ZIP pattern

---

### 5. ImageCreator.cs - Lines 288, 290, 292

**Location:** ZIP file processing in image creation

**Current Code:**
```csharp
foreach (var e in zip)
{
    var name = e.FileName;
    e.Extract(stream);
    Files[e.FileName] = stream;
}
```

**Fixed Code:**
```csharp
foreach (ICSharpCode.SharpZipLib.Zip.ZipEntry e in zip)
{
    var name = e.Name;
    MemoryStream stream = new MemoryStream();
    using (var inputStream = zip.GetInputStream(e))
    {
        inputStream.CopyTo(stream);
    }
    stream.Seek(0, SeekOrigin.Begin);
    Files[e.Name] = stream;
}
```

---

## Helper Extension Method (Optional)

To simplify the code, you can create a helper extension method:

```csharp
namespace GerberLibrary.Core
{
    public static class ZipFileExtensions
    {
        public static void AddFileToZip(this ICSharpCode.SharpZipLib.Zip.ZipOutputStream zipStream, string filePath, string directoryPathInArchive = "")
        {
            byte[] buffer = new byte[4096];
            
            string entryName = string.IsNullOrEmpty(directoryPathInArchive) 
                ? Path.GetFileName(filePath) 
                : Path.Combine(directoryPathInArchive, Path.GetFileName(filePath)).Replace('\\', '/');
            
            ICSharpCode.SharpZipLib.Zip.ZipEntry entry = new ICSharpCode.SharpZipLib.Zip.ZipEntry(entryName);
            entry.DateTime = DateTime.Now;
            
            zipStream.PutNextEntry(entry);
            
            using (FileStream fs = File.OpenRead(filePath))
            {
                int sourceBytes;
                do
                {
                    sourceBytes = fs.Read(buffer, 0, buffer.Length);
                    zipStream.Write(buffer, 0, sourceBytes);
                } while (sourceBytes > 0);
            }
            
            zipStream.CloseEntry();
        }
        
        public static MemoryStream ExtractToMemoryStream(this ICSharpCode.SharpZipLib.Zip.ZipFile zipFile, ICSharpCode.SharpZipLib.Zip.ZipEntry entry)
        {
            MemoryStream ms = new MemoryStream();
            using (var stream = zipFile.GetInputStream(entry))
            {
                stream.CopyTo(ms);
            }
            ms.Seek(0, SeekOrigin.Begin);
            return ms;
        }
    }
}
```

Then use it like:
```csharp
// Writing
using (FileStream fsOut = File.Create(outputPath))
using (var zipStream = new ICSharpCode.SharpZipLib.Zip.ZipOutputStream(fsOut))
{
    zipStream.SetLevel(9);
    zipStream.AddFileToZip(filePath, ".");
    zipStream.Finish();
}

// Reading
using (var zip = new ICSharpCode.SharpZipLib.Zip.ZipFile(path))
{
    foreach (ICSharpCode.SharpZipLib.Zip.ZipEntry e in zip)
    {
        if (!e.IsDirectory)
        {
            var ms = zip.ExtractToMemoryStream(e);
            Files[e.Name] = ms;
        }
    }
}
```

---

## Testing Checklist

After making the fixes:

1. **Compile GerberLibrary.Core**
   ```bash
   cd GerberLibrary.Core
   dotnet build
   ```

2. **Check for errors**
   - Should have 0 errors
   - Warnings are OK

3. **Compile TilingLibrary.Core**
   ```bash
   cd ../TilingLibrary.Core
   dotnet build
   ```

4. **Compile TiNRS-Tiler**
   ```bash
   cd ../TiNRS-Tiler
   dotnet build
   ```

5. **Run TiNRS-Tiler**
   ```bash
   dotnet run
   ```

6. **Test ZIP functionality**
   - Load a ZIP file containing Gerber files
   - Export to ZIP
   - Verify files are readable

---

## Common Issues

### Issue 1: "ZipEntry does not contain a definition for 'FileName'"
**Solution:** Use `e.Name` instead of `e.FileName`

### Issue 2: "ZipEntry does not contain a definition for 'Extract'"
**Solution:** Use `zip.GetInputStream(e)` and `stream.CopyTo()`

### Issue 3: "ZipFile does not contain a constructor that takes 0 arguments"
**Solution:** Use `ZipOutputStream` for writing, `new ZipFile(path)` for reading

### Issue 4: "ZipFile does not contain a definition for 'AddFile'"
**Solution:** Use `ZipOutputStream.PutNextEntry()` and write file contents

### Issue 5: "ZipFile does not contain a definition for 'Save'"
**Solution:** Use `ZipOutputStream.Finish()` and `Close()`

---

## Estimated Time

- **Reading the guide:** 10 minutes
- **Fixing GerberPanel.cs:** 20 minutes
- **Fixing Gerber.cs:** 10 minutes
- **Fixing ImageCreator.cs:** 10 minutes
- **Testing:** 20 minutes
- **Total:** ~70 minutes (1-2 hours with buffer)

---

## Success Criteria

- ✅ All 4 files compile without errors
- ✅ GerberLibrary.Core builds successfully
- ✅ TilingLibrary.Core builds successfully
- ✅ TiNRS-Tiler builds successfully
- ✅ Can load ZIP files containing Gerber files
- ✅ Can export to ZIP files

---

**Good luck! You're almost there!** 🎯
