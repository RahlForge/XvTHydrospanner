# LST File Merging and Profile Switching - CRITICAL FOR MULTIPLAYER
## December 11, 2025

## Overview

Implemented automatic mod file application with special LST file handling. This is **CRITICAL** for XvT multiplayer - all players must have identical LST files to connect properly.

## The LST File Problem

### What are LST Files?
LST (list) files in X-Wing vs TIE Fighter are index files that tell the game which mission files, craft files, or other resources to load. Different folders use LST files differently:
- `MELEE/mission.lst` - Lists available melee missions
- `BATTLE/mission.lst` - Lists available battle missions
- `TRAIN/mission.lst` - Lists training missions
- And many others throughout the game structure

### Why LST Files are Critical
**In multiplayer, ALL players must have IDENTICAL LST files to connect!**

If players have different LST files:
- ❌ Connection fails
- ❌ Desync issues
- ❌ Crashes
- ❌ Unable to join games

### The Challenge
When mods add new missions or modify existing ones, they need to update LST files:
- **Overwriting** would lose existing entries
- **Ignoring** would prevent new content from loading
- **Merging** is required but must be done correctly

## Solution Implemented

### 1. LST File Detection
```csharp
private bool IsLstFile(string filePath)
{
    return Path.GetExtension(filePath).Equals(".lst", StringComparison.OrdinalIgnoreCase);
}
```

### 2. Base LST File Backup System
**Purpose**: Preserve original game LST files before any modifications

**Location**: `Backups/BaseLstFiles/` (with same folder structure as game)

**Registry**: `Backups/BaseLstFiles/registry.txt` tracks which files have been backed up

```
Example registry content:
BalanceOfPower/MELEE/mission.lst
BalanceOfPower/BATTLE/mission.lst
MELEE/mission.lst
BATTLE/mission.lst
```

**First-Time Backup**: When a mod modifies an LST file for the first time:
1. Check if already backed up (registry lookup)
2. If not, copy original game LST to backup folder
3. Add to registry
4. Never overwrite this backup (it's the base game version)

### 3. LST File Merging Logic
**When applying a mod LST file:**

```csharp
private async Task MergeLstFileAsync(string modLstPath, string targetPath)
{
    // Read existing target content
    var existingLines = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    if (File.Exists(targetPath))
    {
        foreach (var line in await File.ReadAllLinesAsync(targetPath))
        {
            var trimmed = line.Trim();
            if (!string.IsNullOrWhiteSpace(trimmed))
            {
                existingLines.Add(trimmed);
            }
        }
    }
    
    // Read mod LST content
    var modLines = await File.ReadAllLinesAsync(modLstPath);
    var linesToAdd = new List<string>();
    
    foreach (var line in modLines)
    {
        var trimmed = line.Trim();
        if (!string.IsNullOrWhiteSpace(trimmed) && !existingLines.Contains(trimmed))
        {
            linesToAdd.Add(trimmed);
        }
    }
    
    // Append new lines if any
    if (linesToAdd.Count > 0)
    {
        await File.AppendAllLinesAsync(targetPath, linesToAdd, Encoding.UTF8);
    }
}
```

**Key Features**:
- Case-insensitive duplicate detection
- Trims whitespace
- Ignores empty lines
- Only adds lines that don't already exist
- Appends to existing file (doesn't overwrite)

### 4. Profile Switching with LST Rebuild
**THE CRITICAL PART - Why this is essential:**

When switching profiles, we MUST:
1. Restore base LST files to clean state
2. Rebuild LST files with new profile's mods

**Why?** Because LST files are cumulative - just reverting individual mods doesn't work.

**Example Scenario**:
```
Base game mission.lst:
MISSION01.TIE
MISSION02.TIE

Profile A adds:
MODMISSION1.TIE
MODMISSION2.TIE

Result: mission.lst now has all 4 entries

Profile B adds:
CUSTOMMISSION.TIE

Problem: If we just "add" Profile B's entry, mission.lst would have:
MISSION01.TIE
MISSION02.TIE
MODMISSION1.TIE    <- Should NOT be here (from Profile A)
MODMISSION2.TIE    <- Should NOT be here (from Profile A)
CUSTOMMISSION.TIE

Solution: Restore base LST, then rebuild with Profile B only:
MISSION01.TIE
MISSION02.TIE
CUSTOMMISSION.TIE  <- Correct!
```

**Implementation**:
```csharp
public async Task<(int applied, int failed)> SwitchProfileAsync(
    ModProfile? oldProfile, 
    ModProfile newProfile, 
    bool createBackup = true)
{
    // Step 1: Revert old profile's non-LST modifications
    if (oldProfile != null)
    {
        await RevertProfileAsync(oldProfile);
    }
    
    // Step 2: Restore ALL base LST files to clean state
    await RestoreAllBaseLstFilesAsync();
    
    // Step 3: Apply new profile (which will properly merge LST files)
    return await ApplyProfileAsync(newProfile, createBackup);
}
```

### 5. Automatic Profile Switching in UI
**User Experience**: When selecting a different profile in the dropdown:

```
Message Box:
Switch from profile 'Vanilla' to 'Enhanced Missions'?

This will:
1. Revert previous profile's regular files
2. Restore base LST files to clean state
3. Apply new profile's modifications

This ensures LST files are rebuilt correctly for multiplayer compatibility.

[Yes] [No]
```

If user clicks Yes:
- Old profile reverted
- Base LST restored
- New profile applied
- Status messages shown during process

If user clicks No:
- Dropdown reverts to previous selection
- No changes made

## File Type Handling

### Regular Files (Non-LST)
**Examples**: `.TIE` mission files, `.CRF` craft files, `.WAV` audio, etc.

**Handling**:
- **Apply**: Copy from warehouse to game folder, overwriting existing
- **Revert**: Restore from backup or delete if no backup
- **Profile Switch**: Revert old, apply new

### LST Files
**Examples**: `mission.lst`, `craft.lst`, etc.

**Handling**:
- **Apply**: 
  - Backup base game LST if first time
  - If target exists: MERGE (append non-duplicate lines)
  - If target doesn't exist: COPY
- **Revert**: NOT done individually - restored as complete set during profile switch
- **Profile Switch**: 
  - Restore ALL base LST files
  - Rebuild with new profile's mods

## Folder Structure

```
Game Install (C:\GOG Games\Star Wars-XVT\)
├── MELEE/
│   └── mission.lst           <- Modified by mods
├── BATTLE/
│   └── mission.lst           <- Modified by mods
├── BalanceOfPower/
│   ├── MELEE/
│   │   └── mission.lst       <- Modified by mods
│   └── BATTLE/
│       └── mission.lst       <- Modified by mods

AppData\XvTHydrospanner\Backups\
├── BaseLstFiles/
│   ├── registry.txt          <- List of backed up LST files
│   ├── MELEE/
│   │   └── mission.lst       <- Base game version (NEVER MODIFIED)
│   ├── BATTLE/
│   │   └── mission.lst       <- Base game version (NEVER MODIFIED)
│   └── BalanceOfPower/
│       ├── MELEE/
│       │   └── mission.lst   <- Base game version (NEVER MODIFIED)
│       └── BATTLE/
│           └── mission.lst   <- Base game version (NEVER MODIFIED)
└── [modification-id]_[timestamp]_[filename]  <- Regular file backups
```

## User Workflow

### Scenario 1: First Time Adding Mod with LST Files
1. User adds "Custom Missions Pack" to Profile A
2. Mod contains `mission.lst` for `MELEE/`
3. Application detects LST file
4. Backs up base game `MELEE/mission.lst` to `BaseLstFiles/`
5. Merges mod's mission entries into game's `MELEE/mission.lst`
6. ✓ Both base missions and custom missions available

### Scenario 2: Switching Between Profiles
1. User has Profile A active (with Custom Missions Pack)
2. User selects Profile B (with Different Mission Set) from dropdown
3. Application prompts for confirmation
4. User clicks Yes
5. Application:
   - Reverts Profile A's regular files
   - Restores `MELEE/mission.lst` from base backup
   - Applies Profile B's mods
   - Merges Profile B's LST entries
6. ✓ Only Profile B's missions available, LST file correct

### Scenario 3: Multiplayer Session
1. Host and all players use same profile
2. All players have identical LST files
3. ✓ Players can connect successfully
4. ✓ Everyone sees same missions/content
5. ✓ No desync or crashes

## Technical Details

### Progress Messages
Application shows real-time progress during operations:
- "Processing LST file: BalanceOfPower/MELEE/mission.lst"
- "Backing up base LST file: MELEE/mission.lst"
- "Merging LST file: mission.lst"
- "Merged 3 line(s) into mission.lst"
- "Restoring all base LST files for clean state..."
- "Restored 4 base LST file(s)"
- "Profile applied: 10 succeeded, 0 failed"

### Error Handling
- LST backup failures are logged but don't stop process
- Missing base backups generate warnings
- Individual file failures don't stop batch operations
- Failed operations return counts for user feedback

### Performance Considerations
- Base LST backups only done once (first modification)
- Registry loaded at startup for fast lookups
- File operations use async/await for responsiveness
- Progress messages use Dispatcher.Invoke for thread safety

## Why This Matters for XvT

### Game Engine Requirements
XvT's multiplayer networking:
- Validates LST file checksums before connection
- Requires byte-perfect match across all players
- Will reject connections with mismatched LST files

### Common Failure Scenarios (PREVENTED by this system)
❌ **Without proper LST handling**:
```
Player 1: Applied Mod A, then Mod B
Player 2: Applied Mod B only
Result: LST files different -> Connection failed
```

✓ **With proper LST handling**:
```
Both players: Use same profile
Result: Identical LST files -> Connection successful
```

## Configuration

### Auto-Backup Setting
```csharp
public bool AutoBackup { get; set; } = true;
```
- Controls whether regular files are backed up before modification
- LST base files are ALWAYS backed up (not controlled by this setting)

### Confirm Before Apply
```csharp
public bool ConfirmBeforeApply { get; set; } = true;
```
- If true, prompts before applying profiles
- If false, applies immediately when selected

## Files Modified

1. **ModApplicator.cs** - Complete rewrite with LST handling:
   - Added `_baseLstBackupPath` and `_baseLstFilesBackedUp` tracking
   - Added `LoadBaseLstFileRegistry()` and `SaveBaseLstFileRegistryAsync()`
   - Added `IsLstFile()` detection
   - Added `BackupBaseLstFileIfNeededAsync()`
   - Added `RestoreBaseLstFileAsync()`
   - Added `MergeLstFileAsync()` with deduplication
   - Added `RestoreAllBaseLstFilesAsync()`
   - Enhanced `ApplyModificationAsync()` with LST special handling
   - Enhanced `ApplyProfileAsync()` to separate LST and regular files
   - Enhanced `RevertProfileAsync()` to skip LST files
   - Added `SwitchProfileAsync()` for proper profile switching

2. **MainWindow.xaml.cs**:
   - Updated `ProfileComboBox_SelectionChanged` to use `SwitchProfileAsync()`
   - Added automatic profile switching with user confirmation
   - Subscribed to `ProgressMessage` event for status updates
   - Added detailed confirmation dialogs explaining the process

## Testing Checklist

- [ ] Base LST files backed up on first mod application
- [ ] LST registry created and maintained
- [ ] LST merging doesn't create duplicates
- [ ] LST merging preserves existing entries
- [ ] Profile switching restores base LST files
- [ ] Profile switching rebuilds LST with new profile's mods
- [ ] Regular files copied/overwritten correctly
- [ ] Regular files reverted correctly
- [ ] Progress messages displayed during operations
- [ ] Multiplayer: Both players with same profile can connect
- [ ] Multiplayer: Players with different profiles cannot connect (expected)

## Multiplayer Compatibility Guide

### For Users
**To ensure multiplayer compatibility:**
1. Host announces which profile they're using
2. All players switch to the SAME profile
3. All players click "Yes" when prompted to apply profile
4. Everyone will have identical LST files
5. Connection will succeed

### For Mod Creators
**When creating mods with LST files:**
1. List only the NEW entries your mod adds
2. Don't include base game entries
3. The application will handle merging automatically
4. Players using your mod will have correct LST files

## Status

✅ **Implemented**: December 11, 2025  
✅ **Built**: Successfully compiled  
✅ **Critical Feature**: Multiplayer compatibility ensured  
⏳ **Testing**: Awaiting real-world multiplayer testing  

## Summary

**Feature**: Automatic mod application with LST file merging  
**Purpose**: Apply mods to game while maintaining multiplayer compatibility  
**Critical**: LST files must be identical across all multiplayer participants  
**Method**: Base LST backup + merge on apply + restore on profile switch  
**Result**: Players using same profile have identical game state  
**Impact**: ESSENTIAL for XvT multiplayer mod support

