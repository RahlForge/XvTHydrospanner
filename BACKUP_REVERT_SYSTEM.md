# Backup and Revert System Documentation
## December 12, 2025

## Overview

The mod management system maintains two separate backup systems to enable complete reverting to base game state: regular file backups and base LST file backups. Understanding both is critical for proper mod management.

## Two-Tier Backup System

### 1. Regular File Backups (Individual .backup Files)

**Purpose**: Back up each mod file individually for easy reversion

**Location**: Next to the modified file
```
Game/BATTLE/BATTLE01.TIE           ← Mod file
Game/BATTLE/BATTLE01.TIE.backup    ← Original backup
```

**Process**:
```csharp
// When applying mod file
var backupPath = targetPath + ".backup";
if (!File.Exists(backupPath))
{
    File.Copy(targetPath, backupPath);  // Save original
}
File.Copy(modFilePath, targetPath, overwrite: true);  // Apply mod
```

**Reversion**:
```csharp
// When reverting
if (File.Exists(backupPath))
{
    File.Copy(backupPath, targetPath, overwrite: true);  // Restore original
    File.Delete(backupPath);  // Clean up backup
}
```

**Characteristics**:
- ✅ One backup per file
- ✅ Easy to identify (same location)
- ✅ Automatic cleanup on revert
- ✅ Simple restore process

### 2. Base LST File Backups (Centralized)

**Purpose**: Maintain clean base game LST files for rebuilding

**Location**: Centralized backup directory
```
Backups/BaseLstFiles/
├── .registry.json                      ← Tracks backed up files
├── MELEE/mission.lst                   ← Base game MELEE mission.lst
├── BalanceofPower/MELEE/mission.lst   ← Base game BoP mission.lst
└── BATTLE/mission.lst                  ← Base game BATTLE mission.lst
```

**Registry Format**:
```json
{
  "BackedUpFiles": [
    "MELEE/mission.lst",
    "BalanceofPower/MELEE/mission.lst",
    "BATTLE/mission.lst"
  ]
}
```

**Why Centralized for LST**:
1. LST files are **merged**, not replaced
2. Need original base game version to rebuild
3. Multiple mods may modify same LST
4. Must restore base before switching profiles

**Backup Process**:
```csharp
private async Task<bool> BackupBaseLstFileIfNeededAsync(string relativePath)
{
    // Check registry
    if (_baseLstFilesBackedUp.Contains(relativePath))
        return true;
    
    var sourcePath = Path.Combine(_gameInstallPath, relativePath);
    var backupPath = Path.Combine(_baseLstBackupPath, relativePath);
    
    // Check if backup already exists on disk (recovery from interruption)
    if (File.Exists(backupPath))
    {
        // Just add to registry
        _baseLstFilesBackedUp.Add(relativePath);
        await SaveBaseLstFileRegistryAsync();
        return true;
    }
    
    // Create backup
    Directory.CreateDirectory(Path.GetDirectoryName(backupPath));
    File.Copy(sourcePath, backupPath, overwrite: false);
    
    // Register
    _baseLstFilesBackedUp.Add(relativePath);
    await SaveBaseLstFileRegistryAsync();
    
    return true;
}
```

**Restore Process**:
```csharp
public async Task<int> RestoreAllBaseLstFilesAsync()
{
    int restored = 0;
    
    // Iterate through registry
    foreach (var relativePath in _baseLstFilesBackedUp.ToList())
    {
        var backupPath = Path.Combine(_baseLstBackupPath, relativePath);
        var targetPath = Path.Combine(_gameInstallPath, relativePath);
        
        if (File.Exists(backupPath))
        {
            File.Copy(backupPath, targetPath, overwrite: true);
            restored++;
        }
    }
    
    return restored;
}
```

## Apply, Revert, and Switch Workflows

### Applying a Profile

**Process**:
```
1. For each file modification:
   a. If LST file:
      - Backup base LST (if not already backed up)
      - Parse and intelligently merge
   b. If regular file:
      - Create .backup of original (if exists)
      - Copy mod file to game directory
   c. Mark modification as applied

2. Save profile state
```

**LST Handling During Apply**:
```csharp
if (IsLstFile(warehouseFile.OriginalFileName))
{
    // Backup base LST first time
    await BackupBaseLstFileIfNeededAsync(relativePath);
    
    // Intelligent merge
    await MergeLstFileAsync(sourceFile, targetPath);
}
```

### Reverting a Profile

**OLD (Broken) Process**:
```csharp
// Only reverted regular files
await _modApplicator.RevertProfileAsync(activeProfile);
// ❌ LST files remained modified!
```

**NEW (Fixed) Process**:
```csharp
// Step 1: Revert regular files
var (success, failed) = await _modApplicator.RevertProfileAsync(activeProfile);

// Step 2: Restore base LST files
await _modApplicator.RestoreAllBaseLstFilesAsync();

// ✅ Complete revert - both regular and LST files
```

**What Happens**:
1. **Regular Files**: Restored from individual .backup files
2. **LST Files**: Restored from centralized base backups
3. **Profile State**: IsApplied = false for all modifications
4. **Result**: Clean base game state

### Switching Profiles

**Process**:
```csharp
public async Task<(int applied, int failed)> SwitchProfileAsync(
    ModProfile? oldProfile, 
    ModProfile newProfile, 
    bool createBackup = true)
{
    // Step 1: Revert old profile's regular files
    if (oldProfile != null)
    {
        await RevertProfileAsync(oldProfile);
    }
    
    // Step 2: CRITICAL - Restore base LST files
    await RestoreAllBaseLstFilesAsync();
    
    // Step 3: Apply new profile (which will merge LSTs cleanly)
    return await ApplyProfileAsync(newProfile, createBackup);
}
```

**Why This Order is Critical**:
1. Old profile's regular files removed
2. **Base LST files restored** (clean slate)
3. New profile's LST files merged into clean base
4. Result: Proper LST structure, no duplicates

**Without LST Restore**:
```
Base game LST: [mission1, mission2]
Apply Profile A: [mission1, mission2, modA1, modA2]
Switch to Profile B WITHOUT restore: [mission1, mission2, modA1, modA2, modB1, modB2]
                                      ^^^^^^^^^^^^^^^^ Should not be here!
```

**With LST Restore**:
```
Base game LST: [mission1, mission2]
Apply Profile A: [mission1, mission2, modA1, modA2]
Restore base LST: [mission1, mission2]  ← Clean slate
Apply Profile B: [mission1, mission2, modB1, modB2]  ← Correct!
```

## Why Two Systems?

### Regular Files: Individual Backups
- Simple replace/revert cycle
- One mod per file
- No merging needed
- Direct restoration

### LST Files: Centralized Backups
- Complex merging required
- Multiple mods can affect one LST
- Need base version to rebuild
- Must restore before reapplying

## Edge Cases and Recovery

### Interrupted Backup

**Scenario**: Backup started but app crashed

**Detection**:
```csharp
// Check both registry AND file system
if (_baseLstFilesBackedUp.Contains(relativePath))
    return true;  // In registry
    
if (File.Exists(backupPath))
{
    // On disk but not in registry - add to registry
    _baseLstFilesBackedUp.Add(relativePath);
    await SaveBaseLstFileRegistryAsync();
    return true;
}
```

**Recovery**: Automatically adds existing backup to registry

### Missing Backup File

**Scenario**: Backup file deleted but still in registry

**Handling**:
```csharp
if (File.Exists(backupPath))
{
    File.Copy(backupPath, targetPath, overwrite: true);
}
else
{
    // Backup missing - warn user but continue
    ProgressMessage?.Invoke(this, $"Warning: Backup not found for {relativePath}");
}
```

### Corrupted Registry

**Scenario**: .registry.json corrupted or deleted

**Recovery**:
```csharp
private async Task LoadBaseLstFileRegistryAsync()
{
    try
    {
        if (File.Exists(_baseLstRegistryPath))
        {
            var json = await File.ReadAllTextAsync(_baseLstRegistryPath);
            var data = JsonConvert.DeserializeObject<BaseLstRegistry>(json);
            _baseLstFilesBackedUp = data?.BackedUpFiles?.ToHashSet() ?? new HashSet<string>();
        }
    }
    catch
    {
        // Corrupted - start fresh
        _baseLstFilesBackedUp = new HashSet<string>();
    }
}
```

**Impact**: Will re-backup on next apply (overwrites old backup)

## User Actions and Expected Results

### Action: Apply Profile with Mods

**Files Modified**:
- Regular mod files → Game directory + .backup created
- LST files → Merged into game directory + base backed up

**Backups Created**:
- `Game/BATTLE01.TIE.backup`
- `Backups/BaseLstFiles/MELEE/mission.lst`

### Action: Revert Profile

**Files Restored**:
- Regular files → From .backup files
- LST files → From base backups

**Cleanup**:
- .backup files deleted
- Base backups remain (for future use)

**Result**: Complete base game restoration

### Action: Reapply Same Profile

**With Old System** (broken):
- LST headers duplicated
- Missions added again
- File grows each time

**With New System** (fixed):
- Intelligent merge detects existing missions
- No duplicates added
- Idempotent operation ✓

### Action: Switch Profiles

**Process**:
1. Revert old profile (regular files)
2. Restore base LST files
3. Apply new profile (including LST merges)

**Result**: Clean switch, no cross-contamination

## Multiplayer Implications

### Why This Matters

**XvT Validation**:
- Checks LST file checksums
- All players must have identical files
- Any difference = connection rejected

**With Proper Backup/Revert**:
- All players apply same profile
- All get same LST files (idempotent merge)
- All can revert to same base state
- Consistent = successful connections ✓

**Without Proper Revert**:
- Player A: Applies → Reverts → Reapplies
- Player B: Just applies once
- LST files different (Player A has duplicates)
- Connection fails ✗

## Testing Checklist

**Backup Creation**:
- [ ] Regular file .backup created on first apply
- [ ] Base LST backup created on first LST modify
- [ ] Registry updated when LST backed up
- [ ] Backup directory structure mirrors game structure

**Reversion**:
- [ ] Regular files restored from .backup
- [ ] LST files restored from base backups
- [ ] .backup files cleaned up after revert
- [ ] Base LST backups remain (not deleted)

**Profile Switch**:
- [ ] Old profile reverted
- [ ] Base LST files restored
- [ ] New profile applied
- [ ] No cross-contamination between profiles

**Idempotent Operations**:
- [ ] Apply → Apply again → No duplicates
- [ ] Apply → Revert → Apply → Same result
- [ ] Switch A→B → Switch B→A → Same as original

**Edge Cases**:
- [ ] Interrupted backup recovered
- [ ] Missing backup file handled gracefully
- [ ] Corrupted registry recovered
- [ ] Partial revert completes successfully

## Documentation

**Related Files**:
1. **LST_FILE_HANDLING.md** - LST-specific backup and merging
2. **INTELLIGENT_LST_MERGING.md** - Parsing and merge algorithm
3. **SESSION_CHANGES.md** - Recent fix documentation
4. **BACKUP_REVERT_SYSTEM.md** (this file) - Complete system overview

## Summary

**Two-Tier System**:
- Regular files: Individual .backup files
- LST files: Centralized base backups

**Critical for Revert**:
- Must restore both regular AND LST files
- LST restoration was missing (now fixed)

**Critical for Switching**:
- Must restore base LST before applying new profile
- Prevents cross-contamination

**Critical for Multiplayer**:
- Idempotent operations ensure consistency
- All players get identical files
- Proper revert enables clean state

**Status**: 
- ✅ System implemented
- ✅ Revert bug fixed  
- ⏳ Awaiting user testing

