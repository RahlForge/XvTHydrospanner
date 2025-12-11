﻿﻿# Session Changes

## December 11, 2025 - Remote Package Download Fix

### 1. Fixed Remote Package Target Path Issue ⚠️ CRITICAL FIX
**Files**: 
- `XvTHydrospanner/Services/ArchiveExtractor.cs` (Lines ~43-70)
- `XvTHydrospanner/Services/RemoteWarehouseManager.cs` (Lines ~145-175)
- `XvTHydrospanner/Services/WarehouseManager.cs` (Lines ~252-295)

**Issue**: When downloading mod packages from the remote library, files were not being placed in their correct target paths, AND filenames were being corrupted with `_1` suffixes. All files were defaulting to the game root with duplicate filenames like `BATTLE01_1.TIE` instead of `BATTLE01.TIE`.

**Example Problem**:
- Expected: 60fps Fix with 3 files in game root + 3 files in BalanceOfPower
- Got: All 6 files in game root, with duplicates named `BATTLE01.TIE`, `BATTLE01_1.TIE`, etc.

**Root Cause**: 
- When uploading, files are stored in ZIP with full paths (e.g., `BalanceOfPower/BATTLE/BATTLE01.TIE`)
- When downloading, TWO problems occurred:
  1. **Archive extraction flattened directory structure**: All files extracted to single temp directory
  2. **Filename collisions created `_1` suffixes**: Files with same name got renamed (BATTLE01.TIE → BATTLE01_1.TIE)
  3. **Catalog metadata was not being passed**: No mapping from archive paths to target locations
- Result: Wrong paths AND wrong filenames

**Solution**:
1. **ArchiveExtractor.cs**: Preserve directory structure during extraction to prevent filename collisions
   - Changed from flat extraction (all files to `temp/`) to structured extraction (`temp/path/to/file.ext`)
   - Eliminated filename collisions at the source
   - Files now keep their original names without `_1` suffixes
   - `OriginalFileName` field stores correct filename

2. **RemoteWarehouseManager.cs**: Extract file location mappings from remote catalog before importing
   - Query catalog for all files belonging to the package
   - Build dictionary mapping ZIP entry paths to target paths
   - Pass this mapping to `AddModPackageFromArchiveAsync`

3. **WarehouseManager.cs**: Enhanced file location lookup with three-level matching strategy
   - Level 1: Match by full archive entry path (e.g., "BalanceOfPower/BATTLE/BATTLE01.TIE")
   - Level 2: Match by normalized path with forward slashes (handles path separators)
   - Level 3: Match by filename only (backward compatibility for older packages)
   - Fallback: Use path detection logic if no match found

**Benefits**:
- ✅ Files placed in correct target directories
- ✅ Filenames preserved correctly (no more `_1` suffixes)
- ✅ Mods work correctly with expected filenames
- ✅ Preserves metadata through upload/download cycle
- ✅ Backward compatible with older package formats
- ✅ Manual imports still work with detection logic

**Documentation**: See `REMOTE_PACKAGE_FIX.md` for detailed technical documentation

### 2. Settings Window Made Scrollable
**File**: `XvTHydrospanner/Views/SettingsWindow.xaml`

**Issue**: When the Remote Repository Settings expander was opened, content extended below the window edge, requiring manual window resizing to see all fields.

**Solution**: 
- Wrapped settings content in a `ScrollViewer` with `VerticalScrollBarVisibility="Auto"`
- Simplified grid structure from 9 rows to 2 rows (content + buttons)
- Buttons remain fixed at bottom, content area scrolls as needed

### 3. Implemented LST File Merging and Automatic Mod Application ⚠️ CRITICAL FOR MULTIPLAYER
**UPDATE**: Added immediate application when adding mods to profile from Mod Library


**Files**:
- `XvTHydrospanner/Services/ModApplicator.cs` (Complete rewrite)
- `XvTHydrospanner/MainWindow.xaml.cs`

**Issue**: Mods were not being applied to the game directory. Additionally, XvT requires identical LST files across all multiplayer players, and simply overwriting or appending LST files would cause desync issues.

**What are LST Files?**: List files that tell XvT which missions, craft, and resources to load. Different in each folder (MELEE/mission.lst, BATTLE/mission.lst, etc.). **ALL players must have IDENTICAL LST files to connect in multiplayer!**

**Solution - LST File Handling**:
1. **Base LST Backup System**:
   - First time an LST file is modified, back up the original game version
   - Store in `Backups/BaseLstFiles/` with same folder structure
   - Maintain registry of backed-up files
   - These backups are NEVER modified (preserve base game state)

2. **LST File Merging**:
   - When applying mod LST file:
     - If target LST exists: MERGE (append only non-duplicate lines)
     - If target doesn't exist: COPY
   - Case-insensitive duplicate detection
   - **CRITICAL: Preserves comment lines starting with `//`**
   - Comment lines define section headers in XvT's in-game drop-down lists
   - Comments always included (even if appearing to duplicate)
   - Preserves existing entries + adds new entries

3. **Profile Switching with LST Rebuild**:
   - Step 1: Revert old profile's regular files
   - Step 2: Restore ALL base LST files to clean state
   - Step 3: Apply new profile (rebuilds LST files correctly)
   - **Why critical**: Ensures only new profile's mods are in LST files

4. **Automatic Mod Application**:
   - When user selects different profile, prompted to apply changes
   - Shows 3-step process explanation
   - Regular files: copied with overwrite
   - LST files: merged intelligently
   - Progress messages show real-time status

5. **Immediate Application from Mod Library**:
   - When adding mod to profile, user is prompted to apply immediately
   - Avoids extra step of clicking "Apply Profile" button
   - Files copied to game directory right away
   - When removing mod, user can choose to revert (but profile re-apply recommended for LST files)

**Multiplayer Impact**:
- ✅ Host and players select same profile
- ✅ All players have identical LST files
- ✅ Players can connect successfully
- ✅ No desync or crashes
- ❌ Without this: Players with different LST files cannot connect

**Benefits**:
- Mods actually applied to game (files copied to game directories)
- LST files handled correctly for multiplayer
- Profile switching ensures clean LST state
- Users see progress during operations
- Automatic process - no manual file management needed

**Documentation**: See `LST_FILE_HANDLING.md` for complete technical details

### 4. Implemented Automatic Page Refresh for Better User Feedback
**Files**:
- `XvTHydrospanner/Views/ModManagementDialog.xaml.cs`
- `XvTHydrospanner/Views/ProfileManagementPage.xaml.cs`
- `XvTHydrospanner/MainWindow.xaml.cs`

**Issue**: After adding/removing mods or creating profiles, the UI didn't update to reflect changes. Users had to navigate away and back to see updates.

**Examples**:
- Adding mod to profile: Green checkmark didn't appear on Mod Library tile
- Removing mod from profile: Green checkmark didn't disappear
- Creating new profile: Profile didn't appear in Profile Management list until navigation refresh

**Solution**:

1. **Mod Library Page Refresh**:
   - Set `DialogResult = true` in ModManagementDialog after adding mods
   - Set `DialogResult = true` in ModManagementDialog after removing mods
   - Existing code in ModLibraryPage checks DialogResult and calls LoadMods()
   - LoadMods() rebuilds view models with updated active status
   - Green checkmark badges update automatically

2. **Profile Management Page Refresh**:
   - Added `Loaded` event handler to refresh profiles when page becomes visible
   - Made `LoadProfiles()` public for external refresh capability
   - Enhanced LoadProfiles() to maintain current selection after refresh
   - MainWindow now refreshes ProfileManagementPage after creating new profile

**Benefits**:
- ✅ Immediate visual feedback - checkmarks appear/disappear instantly
- ✅ No manual refresh needed - automatic updates
- ✅ UI always reflects current state
- ✅ Better user experience - responsive and intuitive
- ✅ Selection maintained where possible (Profile Management)

**User Experience**:
- Add mod → Dialog closes → Mod Library refreshes → Checkmark appears
- Remove mod → Dialog closes → Mod Library refreshes → Checkmark disappears
- Create profile → Profile created → Profile list updates immediately
- Navigate to Profile Management → Always shows latest profiles

### 5. Restructured Profile Management UI for Better Workflow
**Files**:
- `XvTHydrospanner/MainWindow.xaml`
- `XvTHydrospanner/MainWindow.xaml.cs`
- `XvTHydrospanner/Views/ProfileManagementPage.xaml`
- `XvTHydrospanner/Views/ProfileManagementPage.xaml.cs`

**Changes Made**:

1. **Moved New Profile Button**:
   - Removed "New" button from MainWindow header
   - Added "+ New Profile" button to Profile Management page
   - Positioned with Clone and Delete buttons (all profile operations in one place)
   - Creates profile and immediately selects it in the list

2. **Removed Profile Dropdown**:
   - Removed profile selection ComboBox from MainWindow header
   - Replaced with simple text display: "Active Profile: [Name]"
   - Profile switching now happens through Apply Profile button

3. **Enhanced Apply Profile Button**:
   - If on Profile Management page: Uses selected profile from list
   - Sets selected profile as the active profile
   - If switching profiles: Uses SwitchProfileAsync for proper LST handling
   - If same profile: Just applies modifications
   - Automatically updates "Active Profile" display after applying

**Workflow Changes**:

**Before**:
```
- Create profile: Click "New" in header → Enter details
- Switch profile: Select from dropdown → Confirm switch
- Apply mods: Click "Apply Profile"
```

**After**:
```
- Create profile: Go to Profile Management → Click "+ New Profile" → Enter details
- Switch & apply: Go to Profile Management → Select profile → Click "Apply Profile"
- Profile operations all in Profile Management page (New, Clone, Delete)
```

**Benefits**:
- ✅ Consolidated UI - All profile operations in one place
- ✅ Clearer workflow - Apply Profile both switches and applies
- ✅ Simplified header - Just shows active profile name
- ✅ No accidental switches - Must explicitly apply to switch
- ✅ Better for LST files - Switching always uses proper rebuild logic

**User Experience**:
1. User goes to Profile Management
2. Sees list of all profiles
3. Can create new, clone, or delete profiles
4. Selects desired profile
5. Clicks "Apply Profile" button (in left nav)
6. Profile is set as active AND applied to game
7. Header shows "Active Profile: [Name]"

### 6. Improved Profile Management Page Clarity and Usability
**Files**:
- `XvTHydrospanner/Views/ProfileManagementPage.xaml`
- `XvTHydrospanner/Views/ProfileManagementPage.xaml.cs`

**Changes Made**:

1. **Added Active Profile Checkmark Indicator**:
   - Green checkmark (✓) appears next to the currently active profile in the list
   - Uses same color as Mod Library active indicators (#4EC9B0)
   - Updates automatically when profile is applied
   - Profile Management page refreshes automatically when Apply Profile is clicked
   - Checkmark immediately moves to the newly applied profile
   - Provides instant visual feedback of which profile is active

2. **Removed Add Modification Button**:
   - Eliminated "+ Add Modification" button from profile details
   - Removed individual file modification management from this page
   - Mod Library page is now the single place to add/remove mods from profiles
   - Simplifies workflow and reduces redundancy

3. **Changed Display from File Modifications to Mod Packages**:
   - Section now titled "Mod Packages" instead of "File Modifications"
   - Shows the mod packages included in the profile (not individual files)
   - Displays package name, description, file count, and author
   - Added helpful text: "Add or remove mods from the Mod Library page"
   - Groups modifications by package for clearer overview

**Benefits**:
- ✅ Clearer visual indication of active profile
- ✅ Simplified UI - removed redundant modification management
- ✅ Package-level view easier to understand than file-level
- ✅ Consistent with Mod Library workflow
- ✅ Less clutter on Profile Management page

**User Experience**:
- User sees checkmark next to active profile → knows which is active
- User sees list of mod packages → knows what mods are in profile
- User wants to add/remove mods → goes to Mod Library page
- User wants to apply profile → clicks Apply Profile button

**Implementation Details**:
- `LoadModPackagesForProfile()` - Extracts unique package IDs from file modifications
- `UpdateActiveProfileIndicators()` - Shows/hides checkmarks based on active profile
- `FindVisualChild<T>()` - Helper to find checkmark elements in visual tree
- `LoadProfiles()` - Uses priority selection: active profile → previous selection → first item
- Uses `Dispatcher.BeginInvoke` to update checkmarks after UI renders

**Profile Selection Priority**:
1. Active profile is selected first (most relevant to user)
2. Falls back to maintaining previous selection (during refresh)
3. Finally selects first item if no other context available

### 7. Removed Redundant Active Modifications Page
**Files**:
- `XvTHydrospanner/MainWindow.xaml`
- `XvTHydrospanner/MainWindow.xaml.cs`
- `XvTHydrospanner/Views/ModLibraryPage.xaml`
- `XvTHydrospanner/Views/ModLibraryPage.xaml.cs`

**Issue**: The "Active Modifications" page was redundant since the Mod Library page already displays mod information.

**Solution**:
- Removed "Active Modifications" navigation button from main window
- Removed `ActiveModsButton_Click` handler
- Added visual indicator (green checkmark badge) to Mod Library tiles to show which packages are active in the current profile
- Created `ModPackageViewModel` class to track active status for each package
- Enhanced `LoadMods()` to check active profile and mark packages accordingly

**Benefits**:
- Cleaner UI with one less navigation option
- Active status visible at a glance in Mod Library
- No need to switch pages to see which mods are active
- Better user experience with consolidated information

---

## December 10, 2024

## Summary
This session included multiple enhancements to the XvT Hydrospanner mod manager, focusing on the copy-to-game-root feature, remote mod library improvements, and fixing the package download issue.

## Changes Made

### 1. Copy to Game Root Feature for Unstructured Mods
**File**: `XvTHydrospanner/Services/WarehouseManager.cs`

**Issue**: When importing unstructured mods (files without folder structure) and specifying custom file locations through the FileLocationPromptDialog, the "Copy to Game Root" checkbox was not being applied.

**Solution**: Modified the `AddModPackageFromArchiveAsync()` method to apply the `copyToGameRoot` logic to custom file locations. When enabled, for each BalanceOfPower path specified, the system automatically adds an equivalent game root path.

**Example**:
- User selects: `BalanceOfPower/MELEE/mission.tie`
- With checkbox enabled, also creates: `MELEE/mission.tie`

### 2. Remote Mod Library Status Text Fix
**File**: `XvTHydrospanner/Views/RemoteModsPage.xaml.cs`

**Issue**: Status text showed "Loaded 1 Package(s) and 11 file(s)" which was confusing because the package only had 10 files.

**Solution**: Clarified the status message to indicate that the file count represents individual files in the catalog (including files from all packages and standalone files), not just files within displayed packages.

**New status format**: "Loaded {X} package(s) and {Y} individual file(s) from remote catalog"

### 3. Package Archive Upload Implementation
**File**: `XvTHydrospanner/Services/RemoteWarehouseManager.cs`

**Issue**: When downloading packages from the remote warehouse, users received a 404 error because the package .zip archives were never being uploaded to GitHub.

**Root Cause**: The `UploadPackageAsync()` method was only uploading individual files to the `files/` directory and updating the catalog with a reference to a .zip file in `packages/`, but the actual .zip archive was never created or uploaded.

**Solution**: 
1. Added `System.IO.Compression` namespace import
2. Modified `UploadPackageAsync()` to:
   - Upload individual files to `files/` directory (as before)
   - Create a .zip archive containing all package files with their target relative paths
   - Upload the .zip archive to `packages/` directory via new `UploadPackageArchiveAsync()` method
   - Update the catalog with correct package metadata
3. Added `UploadPackageArchiveAsync()` method to handle uploading .zip files to GitHub

### 4. Remote Mod Library UI Modernization
**Files**: 
- `XvTHydrospanner/Views/RemoteModsPage.xaml`
- `XvTHydrospanner/Views/RemoteModsPage.xaml.cs`

**Issue**: Download button text was being cut off due to insufficient width.

**Solution**: 
1. Replaced text-based "Download" button with modern icon button (⬇ download arrow)
2. Styled icon button to match Warehouse page design:
   - Transparent background
   - Blue download arrow (#2196F3)
   - Hover effect (gray background)
   - Disabled state (30% opacity)
   - Tooltip on hover
3. Added right-click context menu to DataGrid with "Download Package" option
4. Reduced Actions column width from 150px to 60px (more efficient use of space)
5. Added `ContextMenu_Download_Click()` handler with proper error handling and status updates

### 5. Documentation Updates
**File**: `ARCHIVE_SUPPORT.md`

Added comprehensive documentation for the Copy to Game Root feature including:
- Overview of the feature
- Implementation details with code example
- User benefits
- Step-by-step example scenario showing how files are automatically duplicated

## Testing Notes

### To Test Copy to Game Root Feature:
1. Import an unstructured mod (archive with files in root, no folders)
2. FileLocationPromptDialog will appear
3. Select BalanceOfPower locations for files
4. Check "Also copy structure to game root" checkbox
5. Confirm import
6. Verify files are created in both BalanceOfPower and game root directories

### To Test Remote Package Download:
1. Navigate to Remote Mod Library
2. Click download arrow icon or right-click and select "Download Package"
3. Package should download successfully (after package archive is uploaded to GitHub)

### To Upload Fixed Package:
1. Navigate to Mod Warehouse page
2. Select the DDraw package
3. Click "Upload to Remote" or use the upload icon
4. Package files AND archive will be uploaded
5. Verify package can now be downloaded from Remote Mod Library

## Future Improvements Suggested
- Add progress bars for package downloads
- Add "View Package Details" context menu option
- Consider adding package preview before download
- Add "Check for Updates" functionality for downloaded packages

