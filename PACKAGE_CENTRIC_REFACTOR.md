# Package-Centric Warehouse Refactor

## Date: December 9, 2025

## Overview
Refactored the warehouse management system to be **package-centric** rather than file-centric. The warehouse now focuses on mod packages as the primary unit of organization for remote synchronization, while individual files remain accessible for profile application.

## Key Changes

### Architecture Changes

#### Before:
- **Warehouse Page**: Listed individual files with mod package as a column
- **Remote Mods Page**: Downloaded individual files
- **Upload**: Uploaded individual files to remote repository
- **Mod Library**: Already package-centric (no changes needed)

#### After:
- **Warehouse Page**: Lists mod packages for remote sync operations
- **Remote Mods Page**: Downloads complete mod packages
- **Upload**: Uploads complete mod packages to remote repository
- **Mod Library**: Remains package-centric (shows local packages for profile application)

### Workflow

```
┌────────────────────────────────────────────────────────────────────┐
│                        USER WORKFLOW                                │
├────────────────────────────────────────────────────────────────────┤
│                                                                     │
│  1. IMPORT MOD PACKAGE                                             │
│     ├─ User: Browse warehouse page                                 │
│     ├─ User: Click "+ Import Mod Package"                          │
│     ├─ User: Select .zip archive                                   │
│     ├─ System: Extract files from archive                          │
│     ├─ System: Detect folder structure or prompt for locations     │
│     ├─ System: Create ModPackage with N files                      │
│     └─ Result: Package appears in Warehouse & Mod Library          │
│                                                                     │
│  2. UPLOAD TO REMOTE                                               │
│     ├─ User: Select package in Warehouse page                      │
│     ├─ User: Click "Upload" or "⬆ Upload to Remote"               │
│     ├─ System: Upload all package files to GitHub                  │
│     ├─ System: Update remote catalog.json with package metadata    │
│     └─ Result: Package available to all users in Remote Mods page  │
│                                                                     │
│  3. DOWNLOAD FROM REMOTE                                           │
│     ├─ User: Browse Remote Mods page                               │
│     ├─ User: Click "Download" on desired package                   │
│     ├─ System: Download package archive from GitHub                │
│     ├─ System: Extract and add to local warehouse                  │
│     └─ Result: Package appears in Warehouse & Mod Library          │
│                                                                     │
│  4. APPLY MOD TO PROFILE                                           │
│     ├─ User: Browse Mod Library                                    │
│     ├─ User: Click "Manage" on a package                           │
│     ├─ User: Add package's files to active profile                 │
│     ├─ User: Apply profile from main window                        │
│     └─ Result: Mod files copied to game directory with backups     │
│                                                                     │
└────────────────────────────────────────────────────────────────────┘
```

## Files Modified

### 1. Views/WarehousePage.xaml
- **Changed**: Grid columns to show package information instead of file information
- **Added**: Package-specific columns (Package Name, Description, Author, Version, Files count)
- **Removed**: File-specific columns (File Name, Category, Target Path, File Size)
- **Changed**: Button text from "+ Add File" to "+ Import Mod Package"
- **Updated**: Header description to clarify remote sync purpose

### 2. Views/WarehousePage.xaml.cs
- **Changed**: `LoadFiles()` to display packages instead of files
- **Removed**: Single file import capability (only archives now)
- **Changed**: `AddFileButton_Click` → `AddPackageButton_Click`
- **Changed**: `DeleteFileButton_Click` → `DeletePackageButton_Click`
- **Added**: `UploadPackageButton_Click` for individual package uploads
- **Changed**: `UploadToRemoteButton_Click` to upload selected package
- **Added**: `UploadPackage()` helper method for package upload logic
- **Changed**: Search functionality to filter packages instead of files
- **Added**: `using System.Threading.Tasks` for async Task methods

### 3. Views/RemoteModsPage.xaml
- **Changed**: Title to "Remote Mod Library - Download Packages"
- **Removed**: Category filter (less relevant at package level)
- **Changed**: Grid name from `RemoteFilesGrid` to `RemotePackagesGrid`
- **Changed**: Grid columns to show package information
- **Added**: "Files" column showing file count per package
- **Removed**: "Size" and "Category" columns (file-level details)
- **Changed**: Button text to "Download All Available Packages"
- **Removed**: "View Packages" button (no longer needed)

### 4. Views/RemoteModsPage.xaml.cs
- **Changed**: Field from `_allRemoteFiles` to `_allRemotePackages`
- **Removed**: `InitializeCategoryFilter()` method
- **Changed**: Event subscription from `FileDownloaded` to `PackageDownloaded`
- **Changed**: `UpdateFilesList()` → `UpdatePackagesList()`
- **Changed**: Search to filter packages instead of files
- **Removed**: Category filter logic
- **Changed**: Download button to download packages instead of files
- **Changed**: Download all to download packages instead of files
- **Removed**: `ViewPackagesButton_Click` method
- **Changed**: Event handler from `OnFileDownloaded` to `OnPackageDownloaded`

### 5. Services/RemoteWarehouseManager.cs
- **Added**: `UploadPackageAsync()` method - Uploads entire package with all files
- **Added**: `UpdateRemoteCatalogWithPackageAsync()` method - Updates remote catalog with package metadata
- **Logic**: Ensures all files in package are uploaded to GitHub
- **Logic**: Maintains file-to-package relationships in remote catalog
- **Logic**: Updates ModPackageId for files belonging to packages

## Data Flow

### Package Import (Local)
```
Archive File (.zip)
    ↓
ArchiveExtractor.ExtractArchive()
    ↓
For each file in archive:
    - Detect folder structure
    - Determine category and target path
    - Create WarehouseFile
    - Associate with ModPackage via ModPackageId
    ↓
ModPackage created with List<FileIds>
    ↓
Saved to local warehouse
    - Files: warehouse/catalog.json
    - Packages: warehouse/packages.json
```

### Package Upload (Local → Remote)
```
ModPackage selected in Warehouse page
    ↓
User clicks "Upload"
    ↓
RemoteWarehouseManager.UploadPackageAsync()
    ↓
For each file in package:
    - Upload file to GitHub: files/{fileId}.ext
    - Update file in catalog.json
    ↓
Update package in catalog.json
    - Add RemoteModPackage entry
    - Set DownloadUrl (placeholder for future)
    - Link FileIds
    ↓
Commit catalog.json to GitHub
    ↓
Package available to all users
```

### Package Download (Remote → Local)
```
RemoteModPackage in Remote Mods page
    ↓
User clicks "Download"
    ↓
RemoteWarehouseManager.DownloadPackageAsync()
    ↓
Download package archive from GitHub
    ↓
Extract archive to temp location
    ↓
WarehouseManager.AddModPackageFromArchiveAsync()
    ↓
Package and files added to local warehouse
    ↓
Package appears in Mod Library
```

### Mod Application (Local → Game)
```
ModPackage in Mod Library
    ↓
User clicks "Manage"
    ↓
User adds files to active profile
    ↓
Profile contains FileModification entries
    ↓
User clicks "Apply Profile" in main window
    ↓
ModApplicator.ApplyProfile()
    ↓
For each FileModification:
    - Backup original game file
    - Copy warehouse file to game directory
    ↓
Mods active in game
```

## Remote Catalog Structure

The remote `catalog.json` now has two main sections:

```json
{
  "Version": "1.0",
  "RepositoryUrl": "https://github.com/owner/repo",
  "Files": [
    {
      "Id": "file-guid",
      "Name": "Mission File",
      "ModPackageId": "package-guid",  ← Links file to package
      "DownloadUrl": "https://raw.githubusercontent.com/.../files/{id}.tie",
      ...other file metadata
    }
  ],
  "Packages": [
    {
      "Id": "package-guid",
      "Name": "Custom Mission Pack",
      "Description": "A collection of custom missions",
      "Author": "Modder Name",
      "Version": "1.0",
      "FileIds": ["file-guid-1", "file-guid-2", ...],  ← Files in package
      "DownloadUrl": "https://raw.githubusercontent.com/.../packages/{id}.zip",
      "IsDownloaded": false
    }
  ]
}
```

## Benefits of Package-Centric Approach

1. **Logical Organization**: Mods are typically distributed as packages, not individual files
2. **Simplified Remote Sync**: Users download/upload entire mods, not individual files
3. **Maintains Relationships**: Files stay associated with their parent package
4. **Better UI/UX**: 
   - Warehouse = Sync interface (packages)
   - Mod Library = Local mods for application (packages)
   - Profiles = Granular file control (files)
5. **Cleaner Catalog**: Remote users see mods as cohesive units
6. **Version Management**: Easier to track versions at package level

## Page Responsibilities

### Warehouse Page
- **Purpose**: Manage packages for remote synchronization
- **Operations**: Import archives, upload packages, delete packages
- **View**: Lists packages with upload capability
- **Audience**: Users managing their local package collection for sharing

### Remote Mods Page  
- **Purpose**: Browse and download packages from remote repository
- **Operations**: Download packages, refresh catalog
- **View**: Lists remote packages with download status
- **Audience**: Users discovering and downloading community mods

### Mod Library Page
- **Purpose**: Manage local mod packages and apply to profiles
- **Operations**: View packages, manage package files, add to profiles
- **View**: Card-based package display with management options
- **Audience**: Users organizing mods for gameplay

## Testing Recommendations

1. **Import Package**: Import a .zip mod archive
   - Verify it appears in both Warehouse and Mod Library
   - Check that all files are extracted correctly

2. **Upload Package**: Upload a package to remote
   - Verify all files are uploaded to GitHub
   - Confirm catalog.json is updated with package entry
   - Check that file-to-package relationships are maintained

3. **Download Package**: Download a package from remote
   - Verify package is downloaded and extracted
   - Confirm it appears in local Warehouse and Mod Library
   - Check that IsDownloaded flag updates

4. **Apply Mod**: Apply a downloaded mod via profile
   - Verify files are copied to correct game locations
   - Confirm backups are created
   - Test revert functionality

## Future Enhancements

1. **Archive Upload**: Upload package archives directly instead of individual files
   - Store package .zip in `packages/` folder on GitHub
   - Include SHA hash for integrity verification
   - Allow re-download of original archive

2. **Dependency Management**: Track dependencies between packages
   - Some mods require other mods to function
   - Display dependency tree before download

3. **Version Updates**: Check for package updates
   - Compare local vs remote versions
   - One-click update functionality

4. **Conflict Detection**: Identify file conflicts
   - Multiple packages modifying same game files
   - Visual indicators in Mod Library

5. **Package Screenshots**: Visual previews of mods
   - Display in Remote Mods and Mod Library
   - Stored in repository or external CDN

## Migration Notes

**No database migration needed** - existing data structures support this refactor:
- `ModPackage` already exists with `FileIds` list
- `WarehouseFile` already has `ModPackageId` field
- `RemoteCatalog` already has both `Files` and `Packages` collections

Existing warehouse data will work without changes. Users may need to:
- Re-import loose files as packages (optional)
- Upload existing packages to remote (one-time operation)

## Build Status
✅ **Build Successful** - Release configuration
✅ **No Warnings**
✅ **No Errors**

## Conclusion

The warehouse is now package-centric for remote operations while maintaining file-level control for profile management. This provides:
- **Intuitive workflow**: Import → Upload → Download → Apply
- **Clear separation**: Warehouse (sync) vs Library (local) vs Profiles (application)
- **Better UX**: Users work with mods as complete packages
- **Flexibility**: Fine-grained control when needed (profiles)

The refactor maintains backward compatibility while improving the user experience for managing and sharing mod packages.
