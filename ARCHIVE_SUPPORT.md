# Archive Support Implementation Summary

## Overview
Implemented support for adding mod files from compressed archives (ZIP, RAR, 7z) to the warehouse. When an archive is selected, it is automatically extracted and all files within are added to the warehouse as a grouped mod package. Mods can be managed through a dedicated Mod Library interface.

## Changes Made

### 1. New Models
- **ModPackage.cs**: New model representing a mod package containing multiple related files
  - Contains metadata (name, description, author, version, tags)
  - Tracks list of file IDs that belong to the package
  
- **WarehouseFile.cs**: Extended with `ModPackageId` property to link files to packages

### 2. New Services
- **ArchiveExtractor.cs**: Service for handling compressed archives
  - `IsArchive()`: Checks if a file is a supported archive format
  - `ExtractArchive()`: Extracts all files from an archive to temp directory
  - `ListArchiveContents()`: Lists files without extracting
  - Supports ZIP, RAR, 7z, TAR, and GZ formats

### 3. Enhanced WarehouseManager
- Added mod package management:
  - `AddModPackageFromArchiveAsync()`: Extracts archive and adds all files as a package
  - `GetAllPackages()`: Get all mod packages
  - `GetPackage()`: Get a specific package by ID
  - `GetPackageFiles()`: Get all files belonging to a package
  - `RemovePackageAsync()`: Remove a package and optionally its files
- Added packages.json file for storing mod package metadata
- Intelligent path detection based on folder structure in archives:
  - Detects known XvT/BoP folders (BATTLE, MELEE, TRAIN, CAMPAIGN, etc.)
  - Automatically maps files to correct game directories
  - Falls back to extension-based logic if no folder structure detected
- Automatic file categorization based on detected folders and extensions

### 4. New UI Components
- **ModLibraryPage.xaml/cs**: Grid-based view of all mod packages
  - Displays mods as tiles with name, description, author, and file count
  - Empty state when no mods are available
  - Clean, modern tile-based layout

- **ModManagementDialog.xaml/cs**: Dialog for managing individual mod packages
  - Add entire mod package to active profile
  - Remove entire mod package from active profile
  - View all files contained in the mod
  - Delete mod package and associated files
  - Shows mod metadata (name, description, author, version)
  - Dynamically enables/disables buttons based on profile state

- **AddModPackageDialog.xaml/cs**: Dialog for adding mod packages from archives
  - Shows archive filename
  - Allows user to enter mod name and description
  - Displays preview of files in the archive
  - Modern dark theme consistent with application

### 5. Updated Components
- **MainWindow.xaml/cs**: Added "Mod Library" navigation button
  - New button in left navigation panel
  - Click handler to navigate to ModLibraryPage

- **WarehousePage.xaml.cs**: Modified `AddFileButton_Click()` to detect archives
  - If archive: Shows AddModPackageDialog
  - If regular file: Shows AddWarehouseFileDialog (existing behavior)
  - Updated file filter to include archive formats

### 6. Dependencies
- **XvTHydrospanner.csproj**: Added SharpCompress package (v0.37.2) for archive handling

## User Workflow

### Adding Mods from Archives
1. User clicks "Add File" button in Warehouse page
2. File dialog now includes "Archives" filter option
3. When user selects an archive file:
   - System detects it's an archive
   - Shows AddModPackageDialog
   - Lists files contained in archive
   - User provides mod name and description
   - All files are extracted and added to warehouse
   - Files are grouped under the mod package
   - Package metadata is saved
4. When user selects a regular file:
   - Shows standard AddWarehouseFileDialog (unchanged behavior)

### Managing Mods
1. User navigates to "Mod Library" from main menu
2. Sees grid of installed mod packages as tiles
3. Clicks "Manage" on a mod tile
4. ModManagementDialog opens with options:
   - **Add to Active Profile**: Adds all mod files to the currently active profile
   - **Remove from Active Profile**: Removes all mod files from the currently active profile
   - **View Files**: Shows list of all files in the mod with their target paths and categories
   - **Delete Mod Package**: Permanently removes the mod and all its files from warehouse

## Intelligent Path Detection

The system now intelligently detects folder structures within archives:

- **Known Folders**: BATTLE, COMBAT, TRAIN, MELEE, CAMPAIGN, TOURN, CP320, CP640, AMOVIE, BMOVIE, MUSIC, WAVE, RESOURCE
- **Automatic Mapping**: Files in `TRAIN` folder → `BalanceOfPower/TRAIN/` in game
- **Base Path Assumption**: Uses `BalanceOfPower/` as base for most mission folders
- **Category Detection**: Categories assigned based on detected folder (more accurate than extension alone)
- **Fallback Logic**: If no folder structure detected, uses extension-based defaults

Example:
- Archive contains: `MELEE/mission1.tie` and `TRAIN/training1.tie`
- Results in: 
  - `mission1.tie` → `BalanceOfPower/MELEE/mission1.tie` (Melee category)
  - `training1.tie` → `BalanceOfPower/TRAIN/training1.tie` (Training category)

## Benefits

- Users can now easily add complete mods distributed as archives
- Files are automatically organized into packages
- Package metadata helps users understand what files belong together
- Easy activation: select the package to add all its files to a profile with one click
- Intelligent path detection respects mod creator's folder organization
- Supports common archive formats used by mod distributors
- Dedicated Mod Library provides central place to manage all installed mods
- Profile integration allows quick addition/removal of entire mods

## File Organization

- Individual files stored in warehouse with unique IDs
- Package metadata stored in packages.json
- Each file knows which package it belongs to via ModPackageId
- Packages can be removed with or without deleting associated files

## Copy to Game Root Feature

### Overview
When importing unstructured mods (files without folder structure in the archive), users can now automatically copy files to both BalanceOfPower and game root directories simultaneously. This ensures compatibility with both XvT and Balance of Power game modes.

### Implementation Details

**Date**: December 10, 2025

**Modified File**: `Services/WarehouseManager.cs`

**Change**: Enhanced the `AddModPackageFromArchiveAsync()` method to apply the `copyToGameRoot` option to custom file locations specified through the FileLocationPromptDialog.

### How It Works

1. **Unstructured Archive Import**: When importing an archive with files that have no folder structure, the FileLocationPromptDialog is shown
2. **User Specifies Locations**: User selects target location(s) for each file (e.g., `BalanceOfPower/MELEE/`)
3. **Copy to Game Root Checkbox**: If the "Also copy structure to game root" checkbox is enabled
4. **Automatic Duplication**: For each BalanceOfPower path specified, the system automatically adds an equivalent game root path
   - Example: `BalanceOfPower/MELEE/mission.tie` → also creates `MELEE/mission.tie`

### Code Logic

```csharp
if (customFileLocations != null && customFileLocations.TryGetValue(fileName, out var customPaths))
{
    targetPaths = new List<string>(customPaths);
    
    // If copyToGameRoot is enabled, also add game root equivalents for BalanceOfPower paths
    if (copyToGameRoot)
    {
        var additionalPaths = new List<string>();
        foreach (var customPath in customPaths)
        {
            if (customPath.StartsWith("BalanceOfPower/", StringComparison.OrdinalIgnoreCase))
            {
                // Add the game root equivalent path
                var gameRootPath = customPath.Substring("BalanceOfPower/".Length);
                additionalPaths.Add(gameRootPath);
            }
        }
        targetPaths.AddRange(additionalPaths);
    }
}
```

### User Benefits

- **Simplified Setup**: No need to manually specify both BalanceOfPower and game root locations
- **Dual Compatibility**: Ensures mods work correctly in both XvT base game and Balance of Power expansion
- **Consistency**: Maintains identical file structure in both locations automatically
- **Time Savings**: One click instead of manually adding each location twice

### Example Scenario

**User imports unstructured archive with files**: `mission1.tie`, `mission2.tie`

**User actions**:
1. Selects `BalanceOfPower/MELEE/` as location for both files in FileLocationPromptDialog
2. Checks "Also copy structure to game root" checkbox
3. Confirms import

**Result**:
Each file is added to warehouse with two target paths:
- `mission1.tie` → `BalanceOfPower/MELEE/mission1.tie` **AND** `MELEE/mission1.tie`
- `mission2.tie` → `BalanceOfPower/MELEE/mission2.tie` **AND** `MELEE/mission2.tie`

When the profile is applied, files are deployed to both locations automatically.

## Next Steps (Future Enhancements)

- Display which mods are currently active in a profile
- Mod update detection and management
- Export packages back to archives
- Mod conflict detection
- Bulk operations on multiple mods
- Search and filter mods in library

