# Rename Mod Feature

## Overview
Added the ability to rename mod packages in the Mod Warehouse.

## Changes Made

### 1. WarehouseManager.cs
- Added `RenamePackageAsync(string packageId, string newName)` method
- Validates that:
  - Package exists
  - New name is not empty
  - No other package already has the new name
- Saves changes to packages.json

### 2. WarehousePage.xaml
- Added "Rename mod" option to the context menu (appears when right-clicking a mod)
- Added a rename button (✏) to the Actions column in the data grid
- Updated Actions column width from 80 to 120 to accommodate the new button

### 3. WarehousePage.xaml.cs
- Added `RenamePackageButton_Click` event handler for the rename button in the grid
- Added `ContextMenu_Rename_Click` event handler for the context menu option
- Added `RenamePackage(ModPackage package)` helper method that:
  - Shows an InputDialog with the current package name pre-filled
  - Validates the new name
  - Calls the WarehouseManager to perform the rename
  - Refreshes the display
  - Updates the details panel if the renamed package is currently selected
  - Shows success/error messages

## User Experience

Users can now rename mod packages in two ways:

1. **Via Action Button**: Click the pencil icon (✏) in the Actions column of any mod
2. **Via Context Menu**: Right-click on any mod and select "Rename mod"

Both methods open an input dialog pre-filled with the current name, allowing easy editing.

### Features:
- Pre-fills current name for easy editing
- Validates that name is not empty
- Prevents duplicate names
- Automatically refreshes the display after renaming
- Updates the details panel if viewing the renamed mod
- Provides clear success/error feedback

## Build Status
✅ Build successful with no errors

