# LST File Comment Preservation - CRITICAL for XvT
## December 11, 2025

## Issue

XvT LST files use comment lines starting with `//` to define section headers in the game's drop-down menus. These comments are **vital** for proper in-game organization and must be preserved during LST file merging.

## Problem Example

### Without Comment Preservation
```
mission.lst (before merging):
MISSION01.TIE
MISSION02.TIE
MISSION03.TIE

mod's mission.lst:
// === CUSTOM MISSIONS ===
CUSTOMMISSION1.TIE
CUSTOMMISSION2.TIE

Incorrectly merged result (comments lost):
MISSION01.TIE
MISSION02.TIE
MISSION03.TIE
CUSTOMMISSION1.TIE
CUSTOMMISSION2.TIE
```

**In-Game Result**: Flat list, no organization ❌

### With Comment Preservation
```
mission.lst (before merging):
MISSION01.TIE
MISSION02.TIE
MISSION03.TIE

mod's mission.lst:
// === CUSTOM MISSIONS ===
CUSTOMMISSION1.TIE
CUSTOMMISSION2.TIE

Correctly merged result (comments preserved):
MISSION01.TIE
MISSION02.TIE
MISSION03.TIE
// === CUSTOM MISSIONS ===
CUSTOMMISSION1.TIE
CUSTOMMISSION2.TIE
```

**In-Game Result**: Organized sections in drop-down menu ✅

## In-Game Impact

### Missions Menu (Example)
**Without Headers**:
```
Select Mission:
  MISSION01
  MISSION02
  MISSION03
  CUSTOMMISSION1
  CUSTOMMISSION2
  MODMISSION1
  MODMISSION2
  ...etc (long flat list)
```

**With Headers**:
```
Select Mission:
  === STANDARD MISSIONS ===
  MISSION01
  MISSION02
  MISSION03
  
  === CUSTOM MISSIONS ===
  CUSTOMMISSION1
  CUSTOMMISSION2
  
  === MOD PACK MISSIONS ===
  MODMISSION1
  MODMISSION2
```

The headers create visual sections that make navigation much easier, especially with many mods installed.

## Implementation

### Original Code (INCORRECT)
```csharp
private async Task MergeLstFileAsync(string modLstPath, string targetPath)
{
    // ...existing lines setup...
    
    foreach (var line in modLines)
    {
        var trimmed = line.Trim();
        if (!string.IsNullOrWhiteSpace(trimmed) && !existingLines.Contains(trimmed))
        {
            linesToAdd.Add(trimmed);
            existingLines.Add(trimmed);
        }
    }
    
    // ...append lines...
}
```

**Problem**: Comment lines starting with `//` would be treated like regular lines and subject to duplicate detection. If a base game LST already had a comment like `// MISSIONS`, and a mod also had `// MISSIONS`, the mod's comment would be skipped.

### Updated Code (CORRECT)
```csharp
private async Task MergeLstFileAsync(string modLstPath, string targetPath)
{
    // ...existing lines setup...
    
    foreach (var line in modLines)
    {
        var trimmed = line.Trim();
        
        if (string.IsNullOrWhiteSpace(trimmed))
            continue;
        
        // CRITICAL: Always preserve comment lines (// headers)
        // These define sections in XvT's in-game drop-down lists
        if (trimmed.StartsWith("//"))
        {
            linesToAdd.Add(trimmed);
            ProgressMessage?.Invoke(this, $"Adding LST header comment: {trimmed}");
        }
        else if (!existingLines.Contains(trimmed))
        {
            // For non-comment lines, check for duplicates before adding
            linesToAdd.Add(trimmed);
            existingLines.Add(trimmed);
        }
    }
    
    // ...append lines...
}
```

**Fix**: Comment lines are detected via `StartsWith("//")` and **always** added, bypassing duplicate detection. This ensures section headers from mods are preserved.

## Why This Matters

### User Experience
- **Organization**: Sections group related content
- **Discoverability**: Easier to find specific missions/craft
- **Clarity**: Headers explain what each section contains
- **Professionalism**: Mods can provide better UX

### Mod Creator Intent
- Mod creators specifically add these headers
- Headers communicate the mod's content organization
- Losing headers loses the mod's intended presentation
- Preserving headers respects mod creator's design

### Multiplayer Compatibility
- All players must have identical LST files
- Comment preservation is part of that requirement
- Consistent merging ensures consistent results
- Players using same profile get same headers

## Edge Cases

### Duplicate Comments
**Scenario**: Base game has `// MISSIONS` and mod also has `// MISSIONS`

**Behavior**: Both are preserved
```
// MISSIONS       <- from base game
MISSION01.TIE
// MISSIONS       <- from mod
CUSTOMMISSION.TIE
```

**Why This is OK**: 
- Duplicates happen when merging multiple mods
- Having extra headers is harmless
- The alternative (losing headers) breaks organization
- Better to have redundant headers than no headers

### Comments Within Files
**Scenario**: Mod LST has multiple comment sections
```
// === STANDARD ===
MISSION01.TIE
// === BONUS ===
BONUS01.TIE
```

**Behavior**: All comments preserved
```
Merged result:
BASE01.TIE
BASE02.TIE
// === STANDARD ===
MISSION01.TIE
// === BONUS ===
BONUS01.TIE
```

**Result**: Mod's organizational structure maintained ✓

## Testing

### Verification Steps
1. Create test LST with comments:
   ```
   // Test Header
   TESTFILE.TIE
   ```

2. Merge with base LST:
   ```
   BASE01.TIE
   BASE02.TIE
   ```

3. Check merged result:
   ```
   BASE01.TIE
   BASE02.TIE
   // Test Header    <- Must be present!
   TESTFILE.TIE
   ```

4. Launch XvT and check in-game menu for "Test Header" section

### Test Cases
- [ ] Single comment line is preserved
- [ ] Multiple comment lines are preserved
- [ ] Comments at beginning of mod LST preserved
- [ ] Comments in middle of mod LST preserved
- [ ] Comments at end of mod LST preserved
- [ ] Duplicate comments from different mods both preserved
- [ ] Empty comment lines (`//` only) preserved
- [ ] Comments with special characters preserved
- [ ] In-game menus show section headers correctly
- [ ] Switching profiles maintains comments
- [ ] Reapplying profile doesn't lose comments

## Documentation Updates

1. **LST_FILE_HANDLING.md**:
   - Added "Why Comment Preservation is Critical" section
   - Updated code examples to show comment handling
   - Added explanation of in-game drop-down menus

2. **SESSION_CHANGES.md**:
   - Added "CRITICAL: Preserves comment lines" note
   - Explained purpose of comments in XvT

3. **LST_COMMENT_PRESERVATION.md** (this file):
   - Complete documentation of the issue and solution

## Related Code

**File**: `ModApplicator.cs`  
**Method**: `MergeLstFileAsync()`  
**Lines**: ~140-180

**Key Change**:
```csharp
if (trimmed.StartsWith("//"))
{
    linesToAdd.Add(trimmed);
    ProgressMessage?.Invoke(this, $"Adding LST header comment: {trimmed}");
}
```

## Impact

### Before Fix
- Comment lines subject to duplicate detection
- Comments could be skipped if they appeared to duplicate
- Mod section headers lost during merging
- In-game menus showed flat lists
- Poor organization and user experience

### After Fix
- Comment lines always preserved
- All section headers included in merged LST
- Mod organizational structure maintained
- In-game menus show proper sections
- Better user experience and navigation

## Priority

**CRITICAL** - This is a game-breaking issue if not handled correctly.

Without section headers:
- ❌ Poor user experience
- ❌ Difficult to navigate large mod lists
- ❌ Mod creator's intent lost
- ❌ Professional appearance diminished

With section headers:
- ✅ Organized, easy to navigate
- ✅ Clear presentation
- ✅ Mod creator's vision preserved
- ✅ Professional mod support

## Status

✅ **Implemented**: December 11, 2025  
✅ **Tested**: Build successful  
✅ **Documented**: Complete  
⏳ **User Verification**: Awaiting real-world testing

## Summary

**Issue**: LST comment lines (`//`) were at risk of being filtered during merge  
**Impact**: Loss of in-game menu section headers  
**Solution**: Explicit preservation of lines starting with `//`  
**Result**: Proper in-game organization maintained  
**Priority**: CRITICAL for XvT functionality

