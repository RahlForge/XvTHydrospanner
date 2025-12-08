# XvT Hydrospanner - Mod Manager for Star Wars: X-Wing vs TIE Fighter

A desktop WPF application for managing game modifications for Star Wars: X-Wing vs TIE Fighter.

## Features

### Profile Management
- Create multiple mod profiles with different configurations
- Switch between profiles easily
- Clone existing profiles
- Each profile maintains its own set of file modifications

### Mod Warehouse
- Centralized repository for storing mod files
- Organize files by category (Missions, Graphics, Sound, etc.)
- Search and filter warehouse contents
- Add metadata to files (author, version, tags, description)

### File Operations
- Automatic backup before applying modifications
- Apply entire profiles or individual modifications
- Revert modifications to restore original files
- Verification system to check if modifications are correctly applied

### User Interface
- Dark-themed, modern interface
- Profile selector in the main window
- Navigation between different sections:
  - Profile Management: Create and edit profiles
  - Mod Warehouse: Browse and manage mod files
  - Active Modifications: View modifications in the current profile
  - Game Files Browser: Explore the game directory structure

## Installation

### Prerequisites
- Windows operating system
- .NET 8.0 Runtime
- Star Wars: X-Wing vs TIE Fighter installed

### Building from Source
1. Open `XvTHydrospanner.sln` in Visual Studio 2022 or later
2. Restore NuGet packages:
   - Newtonsoft.Json (13.0.3)
   - CommunityToolkit.Mvvm (8.2.2)
3. Build the solution (F6)
4. Run the application (F5)

## Usage

### First Run Setup
1. Launch XvTHydrospanner
2. On first run, you'll be prompted to configure:
   - Game Install Path: Point to your Star Wars XvT installation directory
   - Warehouse Path: Location to store mod files (default: AppData)
   - Backup Path: Location for file backups (default: AppData)

### Creating a Profile
1. Click the "New" button next to the profile selector
2. Enter a profile name and optional description
3. Click "Create"

### Adding Files to the Warehouse
1. Navigate to "Mod Warehouse"
2. Click "+ Add File"
3. Select the file to add
4. Fill in metadata:
   - Display Name
   - Target Path (relative to game root)
   - Category
   - Description
5. Click "Add"

### Adding Modifications to a Profile
1. Navigate to "Profile Management"
2. Select a profile from the list
3. Click "+ Add Modification"
4. Select a file from the warehouse
5. Verify/adjust the target path
6. Click "Add"

### Applying a Profile
1. Select the desired profile from the dropdown
2. Click "▶ Apply Profile" button
3. Confirm the operation
4. The application will:
   - Create backups of original files
   - Copy mod files to game directory
   - Update modification status

### Reverting a Profile
1. Select the active profile
2. Click "◀ Revert Profile" button
3. Confirm the operation
4. Original files will be restored from backups

## File Structure

```
XvTHydrospanner/
├── Models/                    # Data models
│   ├── ModProfile.cs
│   ├── FileModification.cs
│   ├── WarehouseFile.cs
│   ├── AppConfig.cs
│   └── GameFileCategory.cs
├── Services/                  # Business logic
│   ├── ProfileManager.cs
│   ├── WarehouseManager.cs
│   ├── ModApplicator.cs
│   └── ConfigurationManager.cs
├── Views/                     # UI pages and dialogs
│   ├── ProfileManagementPage.*
│   ├── WarehousePage.*
│   ├── ActiveModsPage.*
│   ├── GameFilesBrowser.*
│   ├── NewProfileDialog.*
│   ├── SettingsWindow.*
│   ├── AddWarehouseFileDialog.*
│   └── AddModificationDialog.*
├── Styles/                    # WPF styles
│   ├── Colors.xaml
│   └── Buttons.xaml
├── Converters/                # WPF value converters
│   └── BoolToVisibilityConverter.cs
└── Resources/                 # Application resources
```

## Data Storage

Application data is stored in:
```
%APPDATA%\XvTHydrospanner\
├── config.json              # Application configuration
├── Profiles\                # Profile definitions
│   └── {profile-id}.json
├── Warehouse\               # Mod file storage
│   ├── catalog.json         # Warehouse catalog
│   └── {file-id}.{ext}      # Stored mod files
└── Backups\                 # Original file backups
    └── {mod-id}_{timestamp}_{filename}
```

## Game File Categories

The application recognizes these game file categories:
- **Battle Missions**: Battle mode mission files (.TIE, .LST)
- **Combat Missions**: Single combat missions
- **Balance of Power**: Expansion missions (Battle, Campaign, Training, Melee, Tournament)
- **Graphics**: Cockpit and visual files for different resolutions (.LFD, .INT, .PNL)
- **Movies**: Cutscene files (.WRK)
- **Music**: Music files (.VOC, .WAV)
- **Sound Effects**: Sound effect files
- **Resources**: Game resource files (.LFD)
- **Configuration**: Game configuration files (.CFG, .TXT)

## Safety Features

- **Automatic Backups**: Original files are backed up before modification
- **Confirmation Prompts**: User confirmation required before applying changes
- **Profile Isolation**: Each profile maintains separate modifications
- **Verification**: Check if modifications match warehouse versions
- **Revert Capability**: Restore original files from backups

## Troubleshooting

### "Game path not found" error
- Verify the game install path in Settings
- Ensure you're pointing to the root XvT directory

### Modifications not applying
- Check file permissions in the game directory
- Verify warehouse files exist and are not corrupted
- Ensure target paths are correct relative to game root

### Profile won't delete
- Cannot delete the active profile
- Switch to another profile first

## Technical Details

- **Framework**: .NET 8.0 with WPF
- **Architecture**: MVVM-inspired with service layer
- **Data Format**: JSON for configuration and catalogs
- **File Management**: Direct file system operations with backup system

## Future Enhancements

Potential features for future versions:
- Import/export profiles for sharing
- Mod conflict detection
- Automatic game detection
- Batch operations
- Mod update checking
- Integration with online mod repositories
- Mission editor integration
- Graphical diff viewer for modified files

## License

This is a community tool for Star Wars: X-Wing vs TIE Fighter. All Star Wars trademarks and copyrights are property of Lucasfilm Ltd. and Disney.

## Credits

Developed for the Star Wars flight simulator community.

May the Force be with you!
