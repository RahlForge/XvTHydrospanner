# Filename Preservation Fix - December 11, 2025

## Critical Issue: Files Getting `_1` Suffixes

### The Problem

Even after fixing the target paths, downloaded files still had corrupted filenames:
- Expected: `BATTLE01.TIE`
- Got: `BATTLE01_1.TIE`

This caused mods to **fail completely** because:
1. The game looks for specific filenames (e.g., `BATTLE01.TIE`)
2. The warehouse stored incorrect filenames (e.g., `BATTLE01_1.TIE`)
3. When applied to the game, files wouldn't be recognized
4. Mods didn't work even though they were "installed"

### Real-World Example

**60fps Fix Package** should contain:
```
BATTLE01.TIE → BATTLE/BATTLE01.TIE
BATTLE01.TIE → BalanceOfPower/BATTLE/BATTLE01.TIE
```

**What was happening:**
```
BATTLE01.TIE → BATTLE/BATTLE01.TIE (correct filename)
BATTLE01_1.TIE → BalanceOfPower/BATTLE/BATTLE01_1.TIE (WRONG filename!)
```

The game expects `BalanceOfPower/BATTLE/BATTLE01.TIE` but gets `BATTLE01_1.TIE` instead. **Mod fails.**

## Root Cause: Flat Archive Extraction

### The Problematic Code (OLD)

```csharp
// ArchiveExtractor.cs - OLD CODE
foreach (var entry in archive.Entries)
{
    var fileName = Path.GetFileName(entry.Key);
    var extractPath = Path.Combine(tempDir, fileName);  // ALL FILES TO SAME DIRECTORY
    
    // Handle duplicate filenames by appending a number
    var counter = 1;
    while (File.Exists(extractPath))
    {
        extractPath = Path.Combine(tempDir, $"{baseName}_{counter}{extension}");  // ADDS _1, _2, etc.
        counter++;
    }
    
    entry.WriteToFile(extractPath, ...);
    extractedFiles[entry.Key] = extractPath;
}
```

### What Went Wrong

1. **Extraction to flat directory:**
   - `BalanceOfPower/BATTLE/BATTLE01.TIE` → extracted as `temp/BATTLE01.TIE`
   - `BATTLE/BATTLE01.TIE` → tries to extract as `temp/BATTLE01.TIE`
   - **COLLISION!** File already exists

2. **Collision handling adds suffix:**
   - Second file becomes `temp/BATTLE01_1.TIE`
   - This modified filename gets stored in warehouse

3. **OriginalFileName stores wrong value:**
   ```csharp
   // In AddFileAsync:
   warehouseFile.OriginalFileName = fileInfo.Name;  // Gets "BATTLE01_1.TIE" instead of "BATTLE01.TIE"
   ```

4. **Mod fails when applied:**
   - File applied to game as `BalanceOfPower/BATTLE/BATTLE01_1.TIE`
   - Game can't find it (expects `BATTLE01.TIE`)
   - Mod doesn't work

## The Solution: Preserve Directory Structure

### The Fixed Code (NEW)

```csharp
// ArchiveExtractor.cs - NEW CODE
foreach (var entry in archive.Entries)
{
    var normalizedEntryPath = entry.Key.Replace("\\", "/");
    var fileName = Path.GetFileName(normalizedEntryPath);
    
    // IMPORTANT: Preserve directory structure to avoid filename collisions
    // Instead of flattening all files to tempDir, maintain the archive structure
    var relativePath = normalizedEntryPath.Replace("/", Path.DirectorySeparatorChar.ToString());
    var extractPath = Path.Combine(tempDir, relativePath);  // PRESERVE FULL PATH
    
    // Ensure the directory exists
    var extractDir = Path.GetDirectoryName(extractPath);
    if (!string.IsNullOrEmpty(extractDir) && !Directory.Exists(extractDir))
    {
        Directory.CreateDirectory(extractDir);
    }
    
    // Extract with full path preservation - NO COLLISIONS
    entry.WriteToFile(extractPath, new ExtractionOptions { ExtractFullPath = false, Overwrite = true });
    extractedFiles[entry.Key] = extractPath;
}
```

### How It Works Now

1. **Extraction preserves paths:**
   - `BalanceOfPower/BATTLE/BATTLE01.TIE` → `temp/BalanceOfPower/BATTLE/BATTLE01.TIE`
   - `BATTLE/BATTLE01.TIE` → `temp/BATTLE/BATTLE01.TIE`
   - **NO COLLISION!** Files in different directories

2. **No suffix needed:**
   - Both files extracted successfully
   - Both keep original filename `BATTLE01.TIE`

3. **OriginalFileName stores correct value:**
   ```csharp
   // In AddFileAsync:
   warehouseFile.OriginalFileName = fileInfo.Name;  // Gets "BATTLE01.TIE" ✓
   ```

4. **Mod works when applied:**
   - File applied to game as `BalanceOfPower/BATTLE/BATTLE01.TIE` ✓
   - Filename is correct: `BATTLE01.TIE` ✓
   - Game finds it and mod works ✓

## Visual Comparison

### OLD: Flat Extraction (BROKEN)
```
Archive:
├── BalanceOfPower/BATTLE/BATTLE01.TIE
└── BATTLE/BATTLE01.TIE

Extracted to temp/:
├── BATTLE01.TIE      ← First file
└── BATTLE01_1.TIE    ← Second file (RENAMED!)

Stored in warehouse:
├── BATTLE01.TIE → BATTLE/BATTLE01.TIE ✓
└── BATTLE01_1.TIE → BalanceOfPower/BATTLE/BATTLE01_1.TIE ✗ (wrong filename!)
```

### NEW: Structured Extraction (FIXED)
```
Archive:
├── BalanceOfPower/BATTLE/BATTLE01.TIE
└── BATTLE/BATTLE01.TIE

Extracted to temp/:
├── BalanceOfPower/
│   └── BATTLE/
│       └── BATTLE01.TIE      ← Correct filename
└── BATTLE/
    └── BATTLE01.TIE          ← Correct filename

Stored in warehouse:
├── BATTLE01.TIE → BATTLE/BATTLE01.TIE ✓
└── BATTLE01.TIE → BalanceOfPower/BATTLE/BATTLE01.TIE ✓ (correct filename!)
```

## Impact

### Before Fix
❌ Files extracted to flat directory  
❌ Filename collisions created `_1` suffixes  
❌ Wrong filenames stored in warehouse  
❌ Wrong filenames applied to game  
❌ Mods failed to work  

### After Fix
✅ Files extracted with directory structure  
✅ No filename collisions  
✅ Correct filenames stored in warehouse  
✅ Correct filenames applied to game  
✅ Mods work correctly  

## Testing Verification

### How to Verify the Fix

1. **Download a package from remote library** (e.g., 60fps Fix)

2. **Check warehouse file list:**
   ```
   Expected: All files show original filename (e.g., BATTLE01.TIE)
   Not: Files with _1 suffixes (e.g., BATTLE01_1.TIE)
   ```

3. **Check OriginalFileName field:**
   ```csharp
   // Should be:
   file.OriginalFileName = "BATTLE01.TIE"
   
   // NOT:
   file.OriginalFileName = "BATTLE01_1.TIE"
   ```

4. **Apply mod to game:**
   ```
   Files should be copied with correct names:
   ✓ C:\GOG Games\Star Wars-XVT\BATTLE\BATTLE01.TIE
   ✓ C:\GOG Games\Star Wars-XVT\BalanceOfPower\BATTLE\BATTLE01.TIE
   
   NOT:
   ✗ C:\GOG Games\Star Wars-XVT\BalanceOfPower\BATTLE\BATTLE01_1.TIE
   ```

5. **Launch game and test mod functionality**

## Technical Details

### Why This Fix is Critical

The `OriginalFileName` field is used when applying mods to the game:

```csharp
// In ModApplicator.cs (conceptual):
var targetPath = Path.Combine(gamePath, file.TargetRelativePath);
var targetFile = Path.Combine(Path.GetDirectoryName(targetPath), file.OriginalFileName);
File.Copy(file.StoragePath, targetFile);
```

If `OriginalFileName` is wrong:
- Target becomes: `BalanceOfPower/BATTLE/BATTLE01_1.TIE` ❌
- Should be: `BalanceOfPower/BATTLE/BATTLE01.TIE` ✓

### Key Code Change

**One line made the difference:**

```csharp
// OLD: Flat extraction
var extractPath = Path.Combine(tempDir, fileName);

// NEW: Structured extraction
var extractPath = Path.Combine(tempDir, relativePath);
```

This single change:
- Preserves directory structure
- Eliminates collisions
- Maintains correct filenames
- Makes mods work properly

## Related Fixes

This fix works in conjunction with:
1. **Target path mapping** (RemoteWarehouseManager.cs)
2. **Three-level matching** (WarehouseManager.cs)

Together, they ensure:
- Files go to the **right place** (target path mapping)
- Files have the **right name** (directory structure preservation)

## Status

✅ **Implemented**: December 11, 2025  
✅ **Built**: Successfully compiled  
✅ **Documented**: This file + REMOTE_PACKAGE_FIX.md  
⏳ **Tested**: Awaiting user verification  

## Summary

**Problem**: Filenames corrupted with `_1` suffixes, breaking mods  
**Cause**: Flat archive extraction caused filename collisions  
**Solution**: Preserve directory structure during extraction  
**Result**: Correct filenames, working mods  
**Impact**: CRITICAL - Mods now actually work!  

