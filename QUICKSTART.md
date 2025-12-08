# XvT Hydrospanner - Quick Start Guide

## Overview
XvT Hydrospanner is a mod manager that helps you organize and apply modifications to Star Wars: X-Wing vs TIE Fighter. Think of it as a toolkit that keeps your game modifications organized and lets you switch between different configurations easily.

## Key Concepts

### 1. **Profiles**
A profile is a saved configuration that contains a list of file modifications. You can:
- Create multiple profiles for different gameplay experiences
- Switch between profiles without losing your configurations
- Apply or revert entire profiles with one click

**Example Use Cases:**
- "Vanilla" profile: No modifications
- "Enhanced Missions" profile: Custom mission files
- "Graphics Overhaul" profile: Updated cockpit graphics and effects

### 2. **Mod Warehouse**
The Warehouse is your personal library of mod files. It stores:
- Mission files (.TIE, .LST)
- Graphics files (.LFD, .INT, .PNL)
- Sound and music files
- Configuration files
- Any other game files you want to modify

Files in the Warehouse are **safely stored separately** from your game installation.

### 3. **Modifications**
A modification links a file from your Warehouse to a specific location in your game. When you apply a profile:
- The app backs up the original game file
- Copies the mod file from the Warehouse to the game directory
- Tracks the modification so you can revert it later

## Getting Started

### Step 1: Initial Setup
1. **Launch XvTHydrospanner**
2. **Configure Paths** (Settings window will appear on first run):
   - **Game Install Path**: Browse to your XvT installation
     - Example: `C:\GOG Games\Star Wars - XvT`
   - **Warehouse Path**: Leave as default or choose custom location
     - Default: `%APPDATA%\XvTHydrospanner\Warehouse`
   - **Settings**:
     - ✓ Automatically backup files (recommended)
     - ✓ Confirm before applying changes (recommended)
3. Click **Save**

### Step 2: Create Your First Profile
1. Click the **"New"** button (next to profile selector)
2. Enter a name: e.g., "Custom Missions"
3. Optionally add a description
4. Click **Create**

### Step 3: Add Files to the Warehouse
1. Click **"Mod Warehouse"** in the left navigation
2. Click **"+ Add File"**
3. Browse and select a mod file (e.g., a .TIE mission file)
4. Fill in the details:
   - **Display Name**: Friendly name (e.g., "Rebel Assault Mission")
   - **Target Path**: Where it goes in the game
     - Example: `BalanceOfPower/BATTLE/8XA01BXY.TIE`
   - **Category**: Select appropriate category (Mission, Graphics, etc.)
   - **Description**: What this mod does
5. Click **Add**

**Target Path Examples:**
```
BalanceOfPower/BATTLE/mission.lst          # Battle mission list
BalanceOfPower/TRAIN/2TE09BY.TIE           # Training mission
Combat/8B01G01.TIE                          # Combat mission
cp640/XWING12.LFD                           # X-Wing cockpit graphics
Music/track01.wav                           # Music file
```

### Step 4: Add Modifications to Your Profile
1. Click **"Profile Management"** in the left navigation
2. Select your profile from the list
3. Click **"+ Add Modification"**
4. Select a file from the warehouse
5. Verify the target path is correct
6. Click **Add**

Repeat for each file you want to include in this profile.

### Step 5: Apply Your Profile
1. Select your profile from the dropdown at the top
2. Click **"▶ Apply Profile"**
3. Confirm the operation
4. The app will:
   - Backup original files
   - Copy mod files to game
   - Mark modifications as applied

**Your mods are now active!** Launch XvT and enjoy.

### Step 6: Reverting (Going Back to Original)
1. Select the active profile
2. Click **"◀ Revert Profile"**
3. Confirm the operation
4. Original files will be restored

## Workflow Examples

### Scenario 1: Testing a Custom Mission
```
1. Add mission .TIE file to Warehouse
2. Create profile "Test Mission"
3. Add modification pointing to BalanceOfPower/BATTLE/mission.tie
4. Apply profile
5. Test in game
6. Revert when done
```

### Scenario 2: Graphics Mod Pack
```
1. Add all graphic files (.LFD, .INT) to Warehouse
2. Create profile "HD Graphics"
3. Add modifications for each graphic file
4. Apply profile
5. Game now uses enhanced graphics
6. Switch to different profile or revert anytime
```

### Scenario 3: Multiple Campaign Configurations
```
Profile "Imperial Campaign":
- Modified Imperial campaign missions
- Imperial-themed music

Profile "Rebel Campaign":
- Modified Rebel campaign missions
- Rebel-themed music

Switch between them by:
1. Revert current profile
2. Select new profile
3. Apply new profile
```

## Tips and Best Practices

### Organization
- **Use descriptive names** for profiles and warehouse files
- **Group related mods** into the same profile
- **Use categories** to organize your warehouse

### Safety
- **Always backup** before modifying (enabled by default)
- **Test one mod at a time** before creating complex profiles
- **Keep original files** - the app handles backups automatically

### Target Paths
The target path is **relative to the game root**. Examples:
- ✓ `BalanceOfPower/BATTLE/mission.lst`
- ✓ `Combat/8B01G01.TIE`
- ✗ `C:\GOG Games\Star Wars - XvT\Battle\mission.lst` (too specific)

### File Categories Guide
- **Mission**: .TIE mission files
- **Battle/Campaign/Training**: .LST list files and missions
- **Graphics**: .LFD, .INT, .PNL files
- **Sound**: .WAV, .VOC files
- **Configuration**: .CFG, .TXT files

## Troubleshooting

### "Profile won't apply"
- Check that game path is correct in Settings
- Verify you have write permissions to game directory
- Ensure warehouse files still exist

### "Can't delete profile"
- Switch to a different profile first
- Active profile cannot be deleted

### "Modification shows as not applied"
- The file in the game doesn't match the warehouse version
- Reapply the profile to update

### "Game crashes after applying mods"
- Revert the profile to restore original files
- Check mod compatibility with your game version
- Apply mods one at a time to identify the problematic file

## Advanced Features

### Clone Profile
Duplicate an existing profile to create variations:
1. Select profile in Profile Management
2. Click "Clone Profile"
3. Enter new name
4. Modify the cloned profile as needed

### Search Warehouse
Use the search box in Mod Warehouse to quickly find files by:
- Name
- Description
- Original filename

### Manual Backup Cleanup
Backups are stored in: `%APPDATA%\XvTHydrospanner\Backups`
The app keeps the 5 most recent backups per modification by default.

## Data Locations

All application data is stored in:
```
%APPDATA%\XvTHydrospanner\
├── config.json              # Your settings
├── Profiles\                # Profile definitions
├── Warehouse\               # Your mod files
└── Backups\                 # Original file backups
```

To completely reset the app, delete this folder (while app is closed).

## Support and Community

### Common XvT File Locations
```
BalanceOfPower/BATTLE/       # Battle missions
BalanceOfPower/CAMPAIGN/     # Campaign missions
BalanceOfPower/TRAIN/        # Training missions
BalanceOfPower/MELEE/        # Melee missions
BalanceOfPower/TOURN/        # Tournament missions
Combat/                      # Combat missions
cp320/, cp480/, cp640/       # Graphics by resolution
Music/                       # Music files
wave/                        # Sound effects
resource/                    # Game resources
```

### Getting Help
- Check that file paths match the game structure
- Verify file extensions are correct (.TIE, .LST, .LFD, etc.)
- Test with vanilla game first to ensure it works

---

**May the Force be with you, pilot!**
