# XvT Hydrospanner - Project Summary

## Project Overview
XvT Hydrospanner is a comprehensive mod management application for Star Wars: X-Wing vs TIE Fighter, built with C# and WPF (.NET 8.0).

## Architecture

### Technology Stack
- **Framework**: .NET 8.0
- **UI Framework**: WPF (Windows Presentation Foundation)
- **Data Serialization**: Newtonsoft.Json
- **MVVM Support**: CommunityToolkit.Mvvm
- **Platform**: Windows (net8.0-windows)

### Project Structure
```
XvTHydrospanner/
├── XvTHydrospanner.sln              # Visual Studio solution
├── README.md                         # Full documentation
├── QUICKSTART.md                     # User guide
├── Build.ps1                         # Build script
├── .gitignore                        # Git ignore rules
│
└── XvTHydrospanner/                  # Main project
    ├── XvTHydrospanner.csproj        # Project file
    ├── App.xaml / .cs                # Application entry point
    ├── MainWindow.xaml / .cs         # Main application window
    │
    ├── Models/                       # Data models
    │   ├── ModProfile.cs             # Profile definition
    │   ├── FileModification.cs       # Modification entry
    │   ├── WarehouseFile.cs          # Warehouse file metadata
    │   ├── AppConfig.cs              # Application settings
    │   └── GameFileCategory.cs       # Game file categories
    │
    ├── Services/                     # Business logic layer
    │   ├── ProfileManager.cs         # Profile CRUD operations
    │   ├── WarehouseManager.cs       # Warehouse management
    │   ├── ModApplicator.cs          # Apply/revert modifications
    │   └── ConfigurationManager.cs   # Settings management
    │
    ├── Views/                        # UI pages and dialogs
    │   ├── ProfileManagementPage.*   # Profile management UI
    │   ├── WarehousePage.*           # Warehouse browser
    │   ├── ActiveModsPage.*          # Active mods display
    │   ├── GameFilesBrowser.*        # Game directory browser
    │   ├── NewProfileDialog.*        # New profile creation
    │   ├── SettingsWindow.*          # Settings dialog
    │   ├── AddWarehouseFileDialog.*  # Add file to warehouse
    │   └── AddModificationDialog.*   # Add mod to profile
    │
    ├── Converters/                   # WPF value converters
    │   └── BoolToVisibilityConverter.cs
    │
    ├── Styles/                       # WPF styling
    │   ├── Colors.xaml               # Color palette
    │   └── Buttons.xaml              # Button styles
    │
    └── Resources/                    # Application resources
        └── (for icons, images, etc.)
```

## Core Components

### 1. Models Layer

#### ModProfile
- Represents a saved configuration of modifications
- Contains list of FileModification objects
- Tracks creation date, last modified, active status
- Supports custom settings dictionary

#### FileModification
- Links a warehouse file to a game file location
- Tracks application status (applied/not applied)
- Maintains backup path for restoration
- Categorized by ModCategory enum

#### WarehouseFile
- Metadata for files stored in the warehouse
- Includes name, description, category, tags
- Tracks storage path and target path
- Supports author and version information

#### AppConfig
- Application-wide settings
- Paths for game, warehouse, profiles, backups
- User preferences (auto-backup, confirmations)
- Theme settings

#### GameFileCategory
- Predefined game file categories
- Maps to game directory structure
- Includes file extensions for each category

### 2. Services Layer

#### ProfileManager
- Load/save/delete profiles
- Profile activation management
- Clone profile functionality
- Profile CRUD operations
- Event notifications (ProfileActivated, ProfileCreated, etc.)

#### WarehouseManager
- Centralized mod file repository
- Catalog management (JSON-based)
- Search and filter capabilities
- File import/export operations
- Category and tag-based organization

#### ModApplicator
- Apply modifications to game files
- Automatic backup creation
- Revert modifications
- Verify file integrity
- Batch operations (apply/revert entire profiles)
- Cleanup old backups

#### ConfigurationManager
- Load/save application configuration
- Path management
- Settings validation
- Default configuration creation

### 3. Views Layer

#### MainWindow
- Primary application interface
- Navigation system
- Profile selector
- Apply/Revert buttons
- Status bar

#### ProfileManagementPage
- List all profiles
- View profile details
- Add/remove modifications
- Clone/delete profiles

#### WarehousePage
- Browse warehouse files
- Add new files
- Search functionality
- Delete files
- DataGrid display

#### ActiveModsPage
- Display modifications in active profile
- Show application status
- Visual indicators for applied mods

#### GameFilesBrowser
- TreeView of game directory
- Explore game file structure
- Reference for target paths

#### Dialogs
- **NewProfileDialog**: Create new profiles
- **SettingsWindow**: Configure application settings
- **AddWarehouseFileDialog**: Import files to warehouse
- **AddModificationDialog**: Add mods to profile

## Data Flow

### Profile Application Workflow
```
1. User selects profile from dropdown
2. User clicks "Apply Profile"
3. ModApplicator iterates through FileModifications
4. For each modification:
   a. Locate warehouse file
   b. Create backup of original game file
   c. Copy warehouse file to game location
   d. Mark modification as applied
5. Save profile state
6. Update UI
```

### File Addition Workflow
```
1. User adds file via Warehouse UI
2. File copied to warehouse storage
3. Metadata created (WarehouseFile object)
4. Catalog updated and saved
5. File available for use in profiles
```

### Profile Switching Workflow
```
1. User selects new profile
2. System prompts to revert current profile (if applied)
3. New profile set as active
4. Configuration updated
5. UI refreshes to show new profile's modifications
```

## File System Structure

### Application Data
```
%APPDATA%\XvTHydrospanner\
├── config.json                      # Application configuration
├── Profiles\
│   ├── {guid-1}.json               # Profile definition
│   ├── {guid-2}.json
│   └── ...
├── Warehouse\
│   ├── catalog.json                # Warehouse index
│   ├── {guid-1}.TIE               # Stored mod files
│   ├── {guid-2}.LST
│   └── ...
└── Backups\
    ├── {mod-id}_{timestamp}_{file} # Backup files
    └── ...
```

### JSON Structures

#### Profile JSON
```json
{
  "Id": "guid",
  "Name": "Profile Name",
  "Description": "Description",
  "CreatedDate": "2025-12-03T10:00:00",
  "LastModified": "2025-12-03T10:00:00",
  "IsActive": false,
  "FileModifications": [
    {
      "Id": "guid",
      "RelativeGamePath": "BalanceOfPower/BATTLE/mission.lst",
      "WarehouseFileId": "guid",
      "BackupPath": "path",
      "Category": "Battle",
      "IsApplied": false,
      "Description": "Custom mission list"
    }
  ],
  "CustomSettings": {}
}
```

#### Warehouse Catalog JSON
```json
[
  {
    "Id": "guid",
    "Name": "Custom Mission",
    "Description": "Enhanced rebel mission",
    "StoragePath": "path",
    "OriginalFileName": "mission.tie",
    "FileExtension": ".TIE",
    "Category": "Mission",
    "TargetRelativePath": "BalanceOfPower/BATTLE/mission.tie",
    "FileSizeBytes": 12345,
    "DateAdded": "2025-12-03T10:00:00",
    "Tags": ["rebel", "custom"],
    "Author": "Modder Name",
    "Version": "1.0"
  }
]
```

## Key Features

### Safety & Reliability
- Automatic backup before modifications
- User confirmation prompts
- Backup retention (configurable, default 5 versions)
- File verification
- Error handling and recovery

### User Experience
- Dark theme UI
- Intuitive navigation
- Search functionality
- Profile cloning
- Batch operations

### Flexibility
- Multiple profile support
- Category-based organization
- Tag system for warehouse
- Custom settings per profile
- Configurable paths

## Extensibility Points

The application is designed for future enhancements:

### Potential Extensions
1. **Import/Export**
   - Share profiles between installations
   - Backup/restore profiles

2. **Mod Repository Integration**
   - Download mods from online sources
   - Update checking
   - Mod dependencies

3. **Advanced Features**
   - Conflict detection
   - File diff viewer
   - Mission editor integration
   - Automatic game detection

4. **Enhanced UI**
   - Light theme option
   - Custom color schemes
   - Drag-and-drop support
   - Preview images

### Extension Points in Code
- Service interfaces can be abstracted
- Additional ModCategory values
- Custom file validators
- Plugin system for file handlers

## Building & Deployment

### Prerequisites
- Visual Studio 2022 or later
- .NET 8.0 SDK
- Windows 10/11

### Build Process
1. Restore NuGet packages
2. Build solution in Release mode
3. Output: `bin/Release/net8.0-windows/XvTHydrospanner.exe`

### Distribution Options
- **Portable**: Copy entire output folder
- **Installer**: Create MSI with WiX
- **ClickOnce**: For auto-updates
- **Single File**: Publish as single executable

### Build Script
Use included `Build.ps1` for automated build:
```powershell
.\Build.ps1
```

## Testing Strategy

### Manual Testing Checklist
- [ ] Profile creation/deletion
- [ ] Warehouse file addition
- [ ] Modification application
- [ ] Modification reversion
- [ ] Profile switching
- [ ] Settings persistence
- [ ] Backup creation
- [ ] Search functionality
- [ ] Error handling

### Test Scenarios
1. **Clean Install**: First-time setup
2. **Profile Operations**: Full lifecycle
3. **File Operations**: Add, apply, revert
4. **Error Conditions**: Missing files, permissions
5. **Data Persistence**: Config/profile saving

## Known Limitations

1. **Windows Only**: WPF is Windows-specific
2. **Manual Paths**: No automatic game detection
3. **No Conflict Detection**: Users must manage conflicts
4. **Single Game Support**: Designed for XvT only
5. **Local Storage**: No cloud sync

## Security Considerations

### File Operations
- Files copied, not moved (preserves originals)
- Backups created before modifications
- No system file modifications
- Contained to game directory

### Data Storage
- Local file system only
- No network operations
- No elevated privileges required
- User-specific AppData storage

## Performance

### Optimization Areas
- Lazy loading for large warehouses
- Async file operations
- JSON serialization caching
- UI virtualization for large lists

### Resource Usage
- Minimal memory footprint
- No background processes
- File operations on-demand
- Catalog indexing for fast search

## Maintenance

### Configuration Files
- Located in %APPDATA%\XvTHydrospanner
- JSON format (human-readable)
- Can be manually edited if needed
- Automatic migration for future versions

### Backup Management
- Automatic cleanup of old backups
- Configurable retention count
- Manual cleanup option
- Backup location in AppData

## Future Roadmap

### Phase 1: Core Stability
- Bug fixes
- Performance optimization
- User feedback integration

### Phase 2: Enhanced Features
- Profile import/export
- Mod conflict detection
- Automatic game detection
- Enhanced search

### Phase 3: Community Features
- Online mod repository
- Mod sharing
- Update notifications
- Community ratings

### Phase 4: Advanced Tools
- Mission editor integration
- File diff viewer
- Batch processing tools
- Scripting support

## Contributing

### Code Style
- Follow C# naming conventions
- Use async/await for I/O operations
- XML documentation for public APIs
- MVVM patterns where applicable

### Adding Features
1. Create feature branch
2. Implement with tests
3. Update documentation
4. Submit pull request

## License & Legal

- Community tool for Star Wars XvT
- Not affiliated with Lucasfilm/Disney
- Respects game copyrights
- Open for community contributions

---

**Project Status**: Complete and functional
**Version**: 1.0
**Last Updated**: December 3, 2025
