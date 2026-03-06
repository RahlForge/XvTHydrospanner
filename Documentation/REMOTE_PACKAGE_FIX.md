# Remote Package Download Target Path Fix

## Date: December 11, 2025

## Issue Description

When downloading mod packages from the remote library, files were not being placed in their correct target paths. Instead, all files were being placed under the game root directory with duplicate filenames getting `_1`, `_2` suffixes appended.

### Example Problem
The "60fps Fix" package should have contained:
- 3 files under the game root (e.g., `BATTLE/BATTLE01.TIE`)
- 3 files under BalanceOfPower root (e.g., `BalanceOfPower/BATTLE/BATTLE01.TIE`)

But instead, all 6 files were listed in the local warehouse as:
- `BATTLE01.TIE` → game root
- `BATTLE01_1.TIE` → game root (wrong location, should be BoP)
- `BATTLE02.TIE` → game root
- `BATTLE02_1.TIE` → game root (wrong location, should be BoP)
- etc.

## Root Cause Analysis

### The Upload Process (Working Correctly)
1. When uploading a package, files are stored in a ZIP archive
2. Each file is added to the ZIP with its **full `TargetRelativePath`** as the entry name
   - Example: `BalanceOfPower/BATTLE/BATTLE01.TIE`
3. The catalog is updated with each file's metadata including `TargetRelativePath`
4. Remote catalog contains accurate file location information

### The Download Process (Was Broken)
1. Downloaded the ZIP archive from GitHub
2. Called `AddModPackageFromArchiveAsync` **without** passing file location metadata
3. Archive extraction flattened all files to temp directory using only filenames
4. Files with same names from different paths collided:
   - `BalanceOfPower/BATTLE/BATTLE01.TIE` → extracted as `BATTLE01.TIE`
   - `BATTLE/BATTLE01.TIE` → extracted as `BATTLE01_1.TIE` (collision!)
5. Without catalog metadata, path detection logic fell back to defaults
6. All files ended up in wrong locations

### Why This Happened
The code had two separate storage mechanisms that weren't properly coordinated:
- **Zipped packages**: Files stored with full paths as entry names
- **Individual files**: Stored with catalog metadata
- **Problem**: When downloading packages, the mapping from ZIP entry paths to catalog metadata was lost

## Solution Implemented

### Changes Made

#### 1. ArchiveExtractor.cs - `ExtractArchive` Method

**Location**: Lines ~43-70

**Change**: Preserve directory structure during extraction instead of flattening all files

**Problem**: 
- Previously extracted all files to a flat temp directory
- Files with same name from different paths collided (e.g., `BATTLE01.TIE` from multiple directories)
- Collision handling appended `_1`, `_2` suffixes to filenames
- These modified filenames were stored as `OriginalFileName` in warehouse
- Result: Mods failed because expected filenames like `BATTLE01.TIE` became `BATTLE01_1.TIE`

**Solution**:
```csharp
// OLD: Flattened structure
var extractPath = Path.Combine(tempDir, fileName);  // All files go to tempDir root
// Collision: BATTLE01.TIE → temp/BATTLE01.TIE
// Collision: BATTLE01.TIE → temp/BATTLE01_1.TIE (WRONG!)

// NEW: Preserve directory structure
var relativePath = normalizedEntryPath.Replace("/", Path.DirectorySeparatorChar.ToString());
var extractPath = Path.Combine(tempDir, relativePath);
// No collision: BalanceOfPower/BATTLE/BATTLE01.TIE → temp/BalanceOfPower/BATTLE/BATTLE01.TIE
// No collision: BATTLE/BATTLE01.TIE → temp/BATTLE/BATTLE01.TIE
// Both keep original filename: BATTLE01.TIE ✓
```

**Impact**:
- Eliminates filename collisions during extraction
- Files retain their original filenames without `_1` suffixes
- `OriginalFileName` field in warehouse stores correct name
- Mods work correctly because filenames match expectations

#### 2. RemoteWarehouseManager.cs - `DownloadPackageAsync` Method

**Location**: Lines ~145-175

**Change**: Extract file location mapping from remote catalog and pass to warehouse manager

```csharp
// Build file location mapping from catalog
// Map archive entry paths to their target locations
Dictionary<string, List<string>>? customFileLocations = null;
if (_remoteCatalog != null)
{
    // Get all files that belong to this package from the catalog
    var packageFiles = _remoteCatalog.Files.Where(f => f.ModPackageId == remotePackage.Id).ToList();
    
    if (packageFiles.Any())
    {
        customFileLocations = new Dictionary<string, List<string>>();
        
        // In the zip archive, files are stored with their TargetRelativePath as the entry name
        // We need to map from the archive entry path back to the actual target path
        // The archive uses the TargetRelativePath as-is, so we use that as the key
        foreach (var file in packageFiles)
        {
            // The archive entry will be the normalized target path (with forward slashes)
            var archiveEntryPath = file.TargetRelativePath.Replace("\\", "/");
            
            if (!customFileLocations.ContainsKey(archiveEntryPath))
            {
                customFileLocations[archiveEntryPath] = new List<string>();
            }
            
            customFileLocations[archiveEntryPath].Add(file.TargetRelativePath);
        }
    }
}

// Add package to local warehouse with file mappings
var localPackage = await _localWarehouse.AddModPackageFromArchiveAsync(
    tempPath,
    remotePackage.Name,
    remotePackage.Description,
    remotePackage.Author,
    remotePackage.Version,
    remotePackage.Tags,
    customFileLocations  // <-- NEW: Pass the mapping
);
```

**Purpose**: 
- Extracts file location information from the remote catalog
- Creates a mapping from ZIP entry paths to target paths
- Passes this mapping to the warehouse manager

#### 2. WarehouseManager.cs - `AddModPackageFromArchiveAsync` Method

**Location**: Lines ~252-295

**Change**: Enhanced custom file location lookup to match by archive path

```csharp
// Check if custom locations were provided for this file
// First try matching by original archive path (for files stored with full paths)
// Then try matching by filename (for backward compatibility)
List<string>? customPaths = null;
if (customFileLocations != null)
{
    // Try the original path from the archive first (exact match)
    if (customFileLocations.TryGetValue(originalPath, out customPaths) ||
        // Also try with normalized path (forward slashes)
        customFileLocations.TryGetValue(originalPath.Replace("\\", "/"), out customPaths) ||
        // Fall back to filename match for backward compatibility
        customFileLocations.TryGetValue(fileName, out customPaths))
    {
        targetPaths = new List<string>(customPaths);
        
        // ...existing copyToGameRoot logic...
    }
}

if (customPaths == null)
{
    // Determine category and target path based on folder structure and file extension
    var (category, targetPath) = DetermineFilePathAndCategory(originalPath, fileName);
    targetPaths.Add(targetPath);
    
    // ...existing fallback logic...
}
```

**Purpose**:
- Checks archive entry path (full path) first: `BalanceOfPower/BATTLE/BATTLE01.TIE`
- Checks normalized path (forward slashes) second: Same but normalized
- Falls back to filename only for backward compatibility: `BATTLE01.TIE`
- Uses fallback detection logic if no mapping found

### How It Works Now

1. **User downloads package from remote library**
   ```
   User clicks download → RemoteWarehouseManager.DownloadPackageAsync()
   ```

2. **Download and extract catalog metadata**
   ```
   - Download ZIP to temp location
   - Query remote catalog for files with matching package ID
   - Build mapping: {"BalanceOfPower/BATTLE/BATTLE01.TIE" -> ["BalanceOfPower/BATTLE/BATTLE01.TIE"]}
   ```

3. **Extract archive with directory structure preserved**
   ```
   - ArchiveExtractor.ExtractArchive() extracts files with full paths
   - Files extracted to: temp/BalanceOfPower/BATTLE/BATTLE01.TIE and temp/BATTLE/BATTLE01.TIE
   - NO filename collisions, NO _1 suffixes
   - Returns dictionary: {"BalanceOfPower/BATTLE/BATTLE01.TIE" -> "C:\Temp\xyz\BalanceOfPower\BATTLE\BATTLE01.TIE"}
   ```

4. **Match files to target paths**
   ```
   For each extracted file:
     - Archive path: "BalanceOfPower/BATTLE/BATTLE01.TIE"
     - Look up in customFileLocations using archive path
     - Find target: "BalanceOfPower/BATTLE/BATTLE01.TIE"
     - Create warehouse entry with correct target path
   ```

5. **Result: Files in correct locations with correct names**
   ```
   ✓ BATTLE01.TIE → BATTLE/BATTLE01.TIE (filename: BATTLE01.TIE)
   ✓ BATTLE01.TIE → BalanceOfPower/BATTLE/BATTLE01.TIE (filename: BATTLE01.TIE)
   ✓ No more _1 suffixes in filenames
   ✓ Each file knows its proper target location
   ✓ OriginalFileName field stores correct name
   ✓ Mods work correctly with expected filenames
   ```

## Backward Compatibility

The fix maintains backward compatibility with older package formats:

1. **New format** (catalog with file mappings): Uses precise path matching
2. **Old format** (no catalog mapping): Falls back to filename matching and path detection
3. **Manual imports**: Continue to work with existing path detection logic

## Testing Recommendations

1. **Download existing package from remote**
   - Verify files appear with correct target paths
   - No duplicate filenames with `_1` suffixes
   - Files can be applied to correct game directories

2. **Upload and re-download package**
   - Upload a package with files in multiple locations
   - Download it fresh
   - Verify all paths preserved correctly

3. **Import local archive (backward compatibility)**
   - Import a ZIP without catalog metadata
   - Verify fallback path detection still works

## Benefits

✅ **Correct file placement**: Files go to their intended directories  
✅ **Correct filenames**: Files keep original names (BATTLE01.TIE, not BATTLE01_1.TIE)  
✅ **Mods work properly**: Expected filenames match actual filenames  
✅ **No more duplicates**: Eliminates `_1` suffix problem completely  
✅ **Preserves metadata**: Target paths and filenames maintained through upload/download cycle  
✅ **Backward compatible**: Older packages and manual imports still work  
✅ **Clear mapping**: Archive entry paths map directly to target locations  

## Technical Notes

### Archive Entry Path Format
- Upload creates entries like: `BalanceOfPower/BATTLE/BATTLE01.TIE`
- Always uses forward slashes (ZIP standard)
- Full relative path from game root

### Directory Structure Preservation During Extraction
**Critical for preventing filename collisions:**
- Old approach: Extract all files to flat temp directory
  - Result: `temp/BATTLE01.TIE`, `temp/BATTLE01_1.TIE` (collision!)
- New approach: Preserve directory structure from archive
  - Result: `temp/BalanceOfPower/BATTLE/BATTLE01.TIE`, `temp/BATTLE/BATTLE01.TIE` (no collision!)
- Why this matters: `OriginalFileName` is set from extracted file's name
  - Flat extraction: `OriginalFileName = "BATTLE01_1.TIE"` ❌ (mod fails)
  - Structured extraction: `OriginalFileName = "BATTLE01.TIE"` ✅ (mod works)

### Dictionary Key Matching Strategy
1. **Primary**: Original archive entry path (as-is)
2. **Secondary**: Normalized path (backslashes → forward slashes)
3. **Tertiary**: Filename only (backward compatibility)
4. **Fallback**: Path detection from archive structure

### Why Three Matching Strategies?
- **Original path**: Handles exact matches
- **Normalized path**: Handles path separator differences (Windows vs ZIP)
- **Filename only**: Supports older package formats and manual imports
- **Detection**: Last resort for archives without metadata

## Related Files Modified

1. **ArchiveExtractor.cs** (Lines ~43-70)
   - Changed from flat extraction to structured extraction
   - Preserves directory structure from archive
   - Eliminates filename collisions and `_1` suffixes
   - Ensures OriginalFileName field stores correct name

2. **RemoteWarehouseManager.cs** (Lines ~145-175)
   - Added catalog metadata extraction
   - Added file mapping construction
   - Pass mapping to warehouse manager

3. **WarehouseManager.cs** (Lines ~252-295)
   - Enhanced lookup logic for custom file locations
   - Added three-level matching strategy
   - Preserved backward compatibility

## Additional Fix: Scrollable Settings Window

As a bonus fix, the Settings window was also made scrollable to prevent the Remote Repository Settings section from being cut off when expanded.

**File**: `SettingsWindow.xaml`  
**Change**: Wrapped content in `ScrollViewer` with `VerticalScrollBarVisibility="Auto"`

This ensures all settings are accessible even when the repository settings expander is open.

## Status

✅ **Implemented**: December 11, 2025  
✅ **Tested**: Code compiles without errors  
✅ **Documented**: This file  
⏳ **User Testing**: Pending verification with real remote packages  

## Future Improvements

- Add logging for file mapping process
- Display warning if catalog mapping not found
- Add diagnostic view showing file path resolution
- Consider caching catalog file mappings

