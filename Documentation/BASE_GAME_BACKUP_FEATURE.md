# Base Game Install Backup Feature - Implementation Plan

## Overview

This document outlines the implementation of the **Base Game Install Backup** feature. When a user configures their game installation path for the first time, the application will:

1. Create an immutable **Base Game Install profile**
2. Automatically create a **complete backup** of the entire game directory
3. Allow users to **restore** the clean game state at any time
4. Prevent modifications to the backup and profile (immutability enforcement)

---

## Core Specifications

| Aspect | Specification |
|--------|---------------|
| **Backup Location** | `%AppData%\XvTHydrospanner\BaseGameBackup\` |
| **Backup Count** | 1 (single backup only) |
| **Backup Scope** | Entire game directory (all files and folders) |
| **Profile Name** | "Base Game Install" |
| **Profile Properties** | Immutable, Read-Only, cannot be edited or deleted |
| **Overwrite Behavior** | Confirm with user before overwriting existing backup |
| **First-Run Status** | Mandatory — prevent app load if cancelled |
| **Restore Operation** | Equivalent to reverting all mods and switching to Base Game Install profile |

---

## First-Run Flow

### Complete User Journey

```
User Launches App → MainWindow_Loaded()
    ↓
LoadConfig & ValidateConfig()
    ├─ GameInstallPath not set?
    │   └─ Show warning, launch SettingsWindow
    │       └─ User selects game path and closes
    │
    └─ Continue MainWindow_Loaded()
        │
        ├─ Check: IsFirstRun() → profiles.Count == 0
        │
        └─ YES → ShowBackupRequiredDialog()
            │
            ├─ Dialog: "Backup Required"
            ├─ Message: "A backup of your clean game install will be created.
            │           This backup serves as the basis for all mod profiles."
            ├─ "This is a mandatory one-time operation."
            │
            └─ User chooses:
                │
                ├─ [Create Backup] 
                │   │
                │   └─ CreateBaseGameProfileAndBackupAsync()
                │       ├─ Create immutable Base Game Install profile
                │       ├─ Show BackupProgressDialog
                │       ├─ Copy entire game dir → BaseGameBackup/
                │       ├─ Update config (HasBaseGameBackup: true)
                │       ├─ Show success message
                │       └─ Continue to app
                │
                └─ [Cancel]
                    │
                    ├─ ShowConfirmDialog: "Cancel Backup Creation?"
                    ├─ Message: "Backup is required to use this application.
                    │            Without it, you cannot create mod profiles.
                    │            Are you sure you want to cancel?"
                    │
                    └─ User chooses:
                        │
                        ├─ [Continue Without Backup] → Exit app
                        │
                        └─ [Create Backup] → Go back to backup dialog
```

---

## Create New Backup Flow (Settings)

### User Creates Backup from Settings Window

```
User: SettingsWindow → "Create New Backup" button
    ↓
IF backup already exists:
    │
    ├─ ShowConfirmDialog: "Overwrite Existing Backup?"
    ├─ Message: "This action will:"
    ├─   "• Delete the current backup"
    ├─   "• Create a new backup of the current game state"
    ├─   "• This action CANNOT BE UNDONE"
    │
    └─ User chooses:
        │
        ├─ [Cancel] → Exit, no changes
        │
        └─ [Overwrite Backup]
            │
            └─ DeleteOldBackupAsync()
                └─ Delete entire BaseGameBackup/ folder
                    │
                    └─ CreateNewBackupAsync()
                        ├─ Show BackupProgressDialog
                        ├─ Copy game dir → BaseGameBackup/
                        ├─ Update config
                        └─ Show success message
│
ELSE (no backup exists):
    │
    └─ CreateNewBackupAsync() [immediately]
        ├─ Show BackupProgressDialog
        ├─ Copy game dir → BaseGameBackup/
        ├─ Update config
        └─ Show success message
```

---

## Restore Backup Flow (Settings)

### User Restores Game from Backup

```
User: SettingsWindow → "Restore Backup" button
    ↓
ShowConfirmDialog: "Restore Game to Base Game Install?"
    ├─ Message: "WARNING: This will:"
    ├─   "• Restore ALL game files to backup state"
    ├─   "• Remove all applied mods"
    ├─   "• Switch active profile to Base Game Install"
    ├─   "• This action CANNOT BE UNDONE"
    │
    └─ User chooses:
        │
        ├─ [Cancel] → Exit, no restore
        │
        └─ [I Understand - Restore]
            │
            ├─ DeleteAllGameFiles()
            │   └─ Remove entire game directory
            │
            ├─ RestoreBackupAsync()
            │   ├─ Show BackupProgressDialog (mode: Restore)
            │   ├─ Copy BaseGameBackup/ → GameInstallPath/
            │   └─ Update progress
            │
            ├─ RevertAllAppliedMods()
            │   └─ Clear modification states in all profiles
            │
            ├─ SwitchToBaseGameProfile()
            │   ├─ Set Base Game Install as active
            │   ├─ Update UI
            │   └─ Refresh profile display
            │
            └─ ShowSuccessMessage
                └─ "Game restored. Active profile: Base Game Install"
```

---

## Directory Structure After Implementation

### Application Data Layout

```
%AppData%\XvTHydrospanner\
├── config.json                        # Application config (with backup metadata)
├── BaseGameBackup/                    # ← NEW: Single game backup
│   ├── BalanceOfPower/
│   ├── Fonts/
│   ├── Movies/
│   ├── XVTJED.EXE
│   └── ... (entire game directory copy)
│
├── Profiles/
│   ├── {UUID-base-game}.json          # Base Game Install profile (immutable)
│   ├── {UUID-mod-1}.json              # Regular mod profiles
│   └── {UUID-mod-2}.json
│
├── Warehouse/
│   └── catalog.json
└── Backups/
    └── BaseLstFiles/
```

---

## Models & Configuration

### ModProfile.cs - New Properties

```csharp
public class ModProfile
{
    // ... existing properties ...
    
    /// <summary>
    /// True if this is the special Base Game Install profile.
    /// Cannot be edited, deleted, or modified in any way.
    /// </summary>
    public bool IsBaseGameInstall { get; set; } = false;
    
    /// <summary>
    /// True if this profile is immutable (cannot be edited or deleted).
    /// Only applies to Base Game Install profile.
    /// </summary>
    public bool IsImmutable { get; set; } = false;
}
```

### AppConfig.cs - New Properties

```csharp
public class AppConfig
{
    // ... existing properties ...
    
    /// <summary>
    /// Path to the BaseGameBackup folder containing the single game backup.
    /// Default: %AppData%\XvTHydrospanner\BaseGameBackup
    /// </summary>
    public string BaseGameBackupPath { get; set; } = 
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "XvTHydrospanner",
            "BaseGameBackup"
        );
    
    /// <summary>
    /// Indicates whether a Base Game Install backup currently exists.
    /// </summary>
    public bool HasBaseGameBackup { get; set; } = false;
    
    /// <summary>
    /// When the BaseGameBackup was created.
    /// </summary>
    public DateTime? BaseGameBackupCreatedDate { get; set; }
    
    /// <summary>
    /// Size of the backup in bytes.
    /// </summary>
    public long BaseGameBackupSizeBytes { get; set; }
    
    /// <summary>
    /// Number of files in the backup.
    /// </summary>
    public int BaseGameBackupFileCount { get; set; }
}
```

### BackupProgress.cs - New Helper Class

```csharp
/// <summary>
/// Represents progress during backup or restore operations.
/// Used by BackupProgressDialog to display real-time feedback.
/// </summary>
public class BackupProgress
{
    public long BytesCopied { get; set; }
    public long TotalBytes { get; set; }
    public int FilesCopied { get; set; }
    public int TotalFiles { get; set; }
    public string CurrentFile { get; set; }
    public TimeSpan EstimatedTimeRemaining { get; set; }
    public double PercentComplete => TotalBytes > 0 ? (BytesCopied * 100.0 / TotalBytes) : 0;
}
```

### BackupMetadata.cs - New Helper Class

```csharp
/// <summary>
/// Metadata about the Base Game Install backup.
/// </summary>
public class BackupMetadata
{
    public DateTime CreatedDate { get; set; }
    public long SizeBytes { get; set; }
    public int FileCount { get; set; }
}
```

---

## Services Implementation

### BaseGameBackupManager.cs (NEW)

**Responsibilities:**
- Create backup of entire game directory
- Restore game directory from backup
- Validate backup integrity
- Calculate backup size/progress
- Handle cancellation and errors

**Key Methods:**

```csharp
public class BaseGameBackupManager
{
    /// <summary>
    /// Creates a backup of the entire game directory.
    /// If a backup already exists, it should be deleted first by caller.
    /// </summary>
    public async Task<bool> CreateBackupAsync(
        string sourceGamePath,
        string backupPath,
        IProgress<BackupProgress> progress,
        CancellationToken cancellationToken);
    
    /// <summary>
    /// Restores the game directory from the backup.
    /// Assumes target directory is empty or will be overwritten.
    /// </summary>
    public async Task<bool> RestoreBackupAsync(
        string backupPath,
        string targetGamePath,
        IProgress<BackupProgress> progress,
        CancellationToken cancellationToken);
    
    /// <summary>
    /// Checks if a valid backup exists at the given path.
    /// </summary>
    public bool ValidateBackup(string backupPath);
    
    /// <summary>
    /// Gets metadata about the backup (creation date, size, file count).
    /// </summary>
    public BackupMetadata GetBackupMetadata(string backupPath);
    
    /// <summary>
    /// Recursively deletes the backup directory.
    /// </summary>
    public bool DeleteBackup(string backupPath);
}
```

**Implementation Details:**
- Use `DirectoryInfo.EnumerateFileSystemEntries()` for memory efficiency
- Copy files in streams to handle large files without loading into memory
- Report progress on every file completion
- Support cancellation between files
- Handle locked files gracefully (log warning, continue)
- Create backup metadata file for validation

---

### ProfileManager.cs - Modifications

**Enforce Immutability:**

```csharp
public async Task UpdateProfileAsync(ModProfile profile)
{
    var existingProfile = GetProfile(profile.Id);
    
    if (existingProfile?.IsImmutable ?? false)
    {
        throw new InvalidOperationException(
            $"Cannot modify immutable profile '{existingProfile.Name}'. " +
            "Base Game Install profiles cannot be edited directly."
        );
    }
    
    // ... proceed with normal update
}

public async Task DeleteProfileAsync(string profileId)
{
    var profile = GetProfile(profileId);
    
    if (profile?.IsImmutable ?? false)
    {
        throw new InvalidOperationException(
            $"Cannot delete immutable profile '{profile.Name}'. " +
            "Base Game Install backups are protected."
        );
    }
    
    // ... proceed with normal deletion
}
```

**Create Base Game Install Profile:**

```csharp
public async Task<ModProfile> CreateBaseGameProfileAsync()
{
    var baseProfile = new ModProfile
    {
        Name = "Base Game Install",
        Description = "Clean, unmodified base game installation. " +
                     "This profile is immutable and cannot be edited. " +
                     "Use the 'Clone' button to create new mod profiles.",
        IsBaseGameInstall = true,
        IsImmutable = true,
        IsReadOnly = true,
        IsActive = true,
        CreatedDate = DateTime.Now,
        LastModified = DateTime.Now,
        FileModifications = []
    };
    
    await SaveProfileAsync(baseProfile);
    return baseProfile;
}
```

**Clone Base Game Install:**

```csharp
public async Task<ModProfile> CloneBaseGameProfileAsync(string newName)
{
    var baseProfile = _profiles.FirstOrDefault(p => p.IsBaseGameInstall);
    if (baseProfile == null)
        throw new InvalidOperationException("Base Game Install profile not found");
    
    var clone = new ModProfile
    {
        Id = Guid.NewGuid().ToString(),
        Name = newName,
        Description = $"Modded profile (cloned from {baseProfile.Name})",
        IsBaseGameInstall = false,
        IsImmutable = false,
        IsReadOnly = false,
        CreatedDate = DateTime.Now,
        LastModified = DateTime.Now,
        FileModifications = []
    };
    
    await SaveProfileAsync(clone);
    return clone;
}
```

---

### ConfigurationManager.cs - Modifications

**Initialize Backup Path:**

```csharp
private AppConfig CreateDefaultConfig()
{
    var appDataPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "XvTHydrospanner"
    );
    
    return new AppConfig
    {
        GameInstallPath = string.Empty,
        WarehousePath = Path.Combine(appDataPath, "Warehouse"),
        ProfilesPath = Path.Combine(appDataPath, "Profiles"),
        BackupPath = Path.Combine(appDataPath, "Backups"),
        BaseGameBackupPath = Path.Combine(appDataPath, "BaseGameBackup"),  // NEW
        AutoBackup = true,
        ConfirmBeforeApply = true,
        MaxBackupVersions = 5,
        Theme = "Dark"
    };
}
```

---

## Views & Dialogs

### BackupProgressDialog.xaml / BackupProgressDialog.xaml.cs (NEW)

**Features:**
- Progress bar with percent complete
- Current file being copied
- File count display (X of Y files)
- Bytes copied / Total bytes (human-readable formatting)
- ETA (estimated time remaining)
- Cancel button with cancellation token support
- Support for both Backup and Restore modes

**Layout Example:**
```
┌─────────────────────────────────────┐
│   Creating Base Game Install Backup │
├─────────────────────────────────────┤
│ Current File:                       │
│ C:\GOG Games\Star Wars-XVT\...      │
│                                     │
│ [████████░░░░░░░░░░░░░░] 45%        │
│                                     │
│ Files: 1,234 / 2,841                │
│ Size:  456 MB / 1.2 GB              │
│ ETA:   3 minutes remaining          │
│                                     │
│                          [Cancel]   │
└─────────────────────────────────────┘
```

---

### BackupRequiredDialog.xaml / BackupRequiredDialog.xaml.cs (NEW)

**Purpose:**
Inform user that backup is mandatory for first-run setup.

**Layout Example:**
```
┌──────────────────────────────────────┐
│      Backup Required                 │
├──────────────────────────────────────┤
│ A backup of your clean game install  │
│ will be created. This backup serves  │
│ as the basis for all mod profiles.   │
│                                      │
│ This is a mandatory one-time         │
│ operation.                           │
│                                      │
│         [Create Backup] [Cancel]     │
└──────────────────────────────────────┘
```

---

### SettingsWindow.xaml / SettingsWindow.xaml.cs (MODIFY)

**New Section: "Base Game Backup"**

```
┌─ Base Game Backup ────────────────────────────────┐
│                                                   │
│ Status: ✓ Backup Exists                          │
│ Location: %AppData%\XvTHydrospanner\BaseGameBackup│
│ Created:   Jan 15, 2025 @ 6:00 PM (1.2 GB)       │
│ Files:     2,841                                  │
│                                                   │
│ [Create New Backup]    [Restore Backup...]       │
│                                                   │
│ ℹ️ Only 1 backup is kept. Creating a new backup   │
│    will overwrite the existing one.              │
│                                                   │
│ ⚠️ WARNING: Restoring will overwrite all game    │
│    files. Mods will be removed. Cannot be undone.│
│                                                   │
└───────────────────────────────────────────────────┘
```

**Buttons:**
- **"Create New Backup"** - Trigger new backup (with overwrite confirmation if exists)
- **"Restore Backup..."** - Restore game from backup (with warning confirmation)

**Display Updates:**
- Show backup status (exists/doesn't exist)
- Show created date (if exists)
- Show size and file count (if exists)
- Disable "Restore" button if backup doesn't exist

---

### ProfileManagementPage.xaml.cs (MODIFY)

**Enforce Immutability in UI:**

```csharp
private void LoadProfile(ModProfile profile)
{
    if (profile.IsImmutable)
    {
        // Disable all editing controls
        AddModButton.IsEnabled = false;
        RemoveModButton.IsEnabled = false;
        ProfileNameTextBox.IsEnabled = false;
        ProfileDescriptionTextBox.IsEnabled = false;
        DeleteButton.IsEnabled = false;
        
        // Show info banner
        ImmutableProfileBanner.Visibility = Visibility.Visible;
        ImmutableProfileBanner.Text = 
            "This is the Base Game Install profile (immutable). " +
            "You can clone it to create new mod profiles, but cannot edit it directly.";
        
        // Keep these enabled:
        CloneButton.IsEnabled = true;
        ApplyButton.IsEnabled = false; // Still can't apply (IsReadOnly)
    }
    else
    {
        // Normal editable profile
        AddModButton.IsEnabled = true;
        RemoveModButton.IsEnabled = true;
        DeleteButton.IsEnabled = true;
        // ... etc
    }
}
```

---

### MainWindow.xaml.cs (MODIFY)

**First-Run Detection and Backup Trigger:**

```csharp
private bool IsFirstRun()
{
    var baseProfile = _profileManager.GetAllProfiles()
        .FirstOrDefault(p => p.IsBaseGameInstall);
    return baseProfile == null;
}

private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
{
    var config = await _configManager.LoadConfigAsync();
    
    var (isValid, errors) = _configManager.ValidateConfig();
    if (isValid == false)
    {
        // Show settings dialog for game path selection
        var settingsWindow = new SettingsWindow(_configManager);
        if (settingsWindow.ShowDialog() != true)
        {
            Application.Current.Shutdown();
            return;
        }
        config = _configManager.GetConfig();
    }
    
    // Initialize managers
    // ... (existing code) ...
    
    // Check if first-run and show backup requirement
    if (IsFirstRun())
    {
        await ShowBackupRequiredDialogAsync();
    }
    
    // ... (rest of initialization) ...
}

private async Task ShowBackupRequiredDialogAsync()
{
    bool? result = null;
    
    while (result == null)
    {
        var backupDialog = new BackupRequiredDialog();
        result = backupDialog.ShowDialog();
        
        if (result == true)
        {
            // User chose to create backup
            await CreateBaseGameProfileAndBackupAsync();
            break;
        }
        else if (result == false)
        {
            // User chose to cancel - confirm they want to exit
            var confirmDialog = MessageBox.Show(
                "Backup is required to use this application.\n\n" +
                "Without it, you cannot create mod profiles.\n\n" +
                "Are you sure you want to cancel?",
                "Cancel Backup Creation?",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question
            );
            
            if (confirmDialog == MessageBoxResult.Yes)
            {
                // Exit app
                Application.Current.Shutdown();
                return;
            }
            else
            {
                // Go back to backup dialog
                result = null;
            }
        }
    }
}

private async Task CreateBaseGameProfileAndBackupAsync()
{
    // 1. Create immutable Base Game Install profile
    var baseProfile = await _profileManager.CreateBaseGameProfileAsync();
    
    // 2. Show progress dialog and create backup
    var progressDialog = new BackupProgressDialog();
    progressDialog.Show();
    
    using var cts = new CancellationTokenSource();
    progressDialog.CancellationToken = cts.Token;
    
    var progress = new Progress<BackupProgress>(p =>
    {
        progressDialog.UpdateProgress(p);
    });
    
    bool success = await _backupManager.CreateBackupAsync(
        _configManager.GetConfig().GameInstallPath,
        _configManager.GetConfig().BaseGameBackupPath,
        progress,
        cts.Token
    );
    
    if (success)
    {
        var metadata = _backupManager.GetBackupMetadata(
            _configManager.GetConfig().BaseGameBackupPath
        );
        
        var config = _configManager.GetConfig();
        config.HasBaseGameBackup = true;
        config.BaseGameBackupCreatedDate = DateTime.Now;
        config.BaseGameBackupSizeBytes = metadata.SizeBytes;
        config.BaseGameBackupFileCount = metadata.FileCount;
        await _configManager.SaveConfigAsync();
        
        progressDialog.Close();
        
        MessageBox.Show(
            "Base Game Install backup created successfully!\n\n" +
            "✓ Profile: Base Game Install (protected)\n" +
            "✓ Backup: Stored locally\n\n" +
            "You can now:\n" +
            "• Clone the Base Game Install to create mod profiles\n" +
            "• Apply mods through new profiles\n" +
            "• Restore the clean game state anytime via Settings",
            "Setup Complete",
            MessageBoxButton.OK,
            MessageBoxImage.Information
        );
    }
    else
    {
        progressDialog.Close();
        
        MessageBox.Show(
            "Failed to create backup. Please check disk space and try again.",
            "Backup Failed",
            MessageBoxButton.OK,
            MessageBoxImage.Error
        );
    }
}
```

---

## Implementation Files Summary

| File | Type | Changes | LOC |
|------|------|---------|-----|
| `Models/ModProfile.cs` | MODIFY | Add IsBaseGameInstall, IsImmutable | +2 properties |
| `Models/AppConfig.cs` | MODIFY | Add backup tracking properties | +6 properties |
| `Models/BackupProgress.cs` | CREATE | Progress tracking class | ~30 |
| `Models/BackupMetadata.cs` | CREATE | Backup metadata class | ~20 |
| `Services/BaseGameBackupManager.cs` | CREATE | Backup/restore service | 300-400 |
| `Services/ProfileManager.cs` | MODIFY | Enforce immutability, create base profile | +60-80 |
| `Services/ConfigurationManager.cs` | MODIFY | Initialize backup paths | +15-20 |
| `MainWindow.xaml.cs` | MODIFY | First-run backup trigger | +80-100 |
| `Views/BackupProgressDialog.xaml` | CREATE | Progress UI | ~50-60 |
| `Views/BackupProgressDialog.xaml.cs` | CREATE | Progress logic | 100-150 |
| `Views/BackupRequiredDialog.xaml` | CREATE | First-run requirement dialog | ~40-50 |
| `Views/BackupRequiredDialog.xaml.cs` | CREATE | First-run dialog logic | ~50-80 |
| `Views/SettingsWindow.xaml` | MODIFY | Backup management section | +30-40 |
| `Views/SettingsWindow.xaml.cs` | MODIFY | Backup button handlers | +60-80 |
| `Views/ProfileManagementPage.xaml.cs` | MODIFY | Immutability UI enforcement | +30-40 |
| **Documentation** | UPDATE | Reflect new feature | - |

---

## Testing Checklist

### First-Run Setup
- [ ] Launch app first time → Create Base Game Install profile and backup
- [ ] Cancel backup → Show confirmation dialog
- [ ] Confirm cancel → Exit app
- [ ] Cancel, then go back to create → Backup proceeds normally
- [ ] Backup completes → Profile created and marked immutable
- [ ] Base Game Install profile visible in profile list

### Profile Management
- [ ] Base Game Install profile → Cannot edit name
- [ ] Base Game Install profile → Cannot add/remove mods
- [ ] Base Game Install profile → Cannot delete profile
- [ ] Base Game Install profile → Clone button works, creates editable clone
- [ ] Cloned profile → Fully editable

### Settings: Create New Backup
- [ ] No backup exists → Create button creates backup immediately
- [ ] Backup exists → Show overwrite confirmation
- [ ] Confirm overwrite → Delete old backup, create new one
- [ ] Cancel overwrite → No changes made
- [ ] Backup created → Update display with metadata
- [ ] Large game directory → Progress dialog shows accurate progress

### Settings: Restore Backup
- [ ] No backup exists → Restore button disabled
- [ ] Backup exists → Restore button enabled
- [ ] Click restore → Show warning confirmation
- [ ] Confirm restore → Copy backup to game directory
- [ ] Restore complete → Switch to Base Game Install profile
- [ ] Mods reverted → Applied mods no longer active

### Error Handling
- [ ] Insufficient disk space → Show warning before backup
- [ ] Locked files during backup → Log warning, continue
- [ ] Locked files during restore → Log warning, continue
- [ ] Cancellation during backup → Stop copying, delete partial backup
- [ ] Cancellation during restore → Stop copying
- [ ] Invalid backup path → Show error, prevent restore

### UI/UX
- [ ] Progress dialog shows current file being copied
- [ ] Progress dialog shows accurate percent complete
- [ ] Progress dialog shows ETA
- [ ] Progress dialog cancel button works
- [ ] Immutability banner visible for Base Game Install
- [ ] Backup section in Settings shows status and metadata
- [ ] Human-readable file sizes (B, KB, MB, GB)

---

## Implementation Order (Recommended)

### **Phase 1: Core Models & Initialization (1-2 hours)**
1. Modify `AppConfig.cs` - Add backup properties
2. Modify `ModProfile.cs` - Add immutability flags
3. Create `BackupProgress.cs` helper class
4. Create `BackupMetadata.cs` helper class
5. Modify `ConfigurationManager.cs` - Initialize backup path

### **Phase 2: Backup Service (2-3 hours)**
6. Create `BaseGameBackupManager.cs` - Core backup/restore logic
7. Implement recursive directory copy with progress reporting
8. Implement cancellation support
9. Implement validation and metadata calculation

### **Phase 3: Profile Protection (1 hour)**
10. Modify `ProfileManager.cs` - Add immutability enforcement
11. Add `CreateBaseGameProfileAsync()` method
12. Add `CloneBaseGameProfileAsync()` method

### **Phase 4: UI Components (2-3 hours)**
13. Create `BackupProgressDialog.xaml` and `.xaml.cs`
14. Create `BackupRequiredDialog.xaml` and `.xaml.cs`
15. Modify `SettingsWindow.xaml` - Add backup section
16. Modify `SettingsWindow.xaml.cs` - Add backup handlers

### **Phase 5: Main Window Integration (1-2 hours)**
17. Modify `MainWindow.xaml.cs` - Add first-run detection
18. Implement backup trigger on first-run
19. Implement cancel confirmation flow

### **Phase 6: Profile Management UI (1 hour)**
20. Modify `ProfileManagementPage.xaml.cs` - Enforce immutability in UI

### **Phase 7: Testing & Documentation (2-3 hours)**
21. Manual testing of all flows
22. Test error handling and edge cases
23. Update `Documentation/` with new feature details

**Total Estimated Time: 10-15 hours**

---

## Edge Cases & Error Handling

### Insufficient Disk Space
**Before Backup Creation:**
- Calculate total backup size
- If insufficient space: Show warning dialog with required/available space
- Allow user to cancel

### Locked Files
**During Backup/Restore:**
- Log warning for locked files
- Skip locked files and continue
- Show notification with count of skipped files
- Backup still succeeds (user warned)

### Cancellation
**During Backup:**
- Delete partially-created backup folder
- Clear backup metadata from config
- Return to previous state

**During Restore:**
- Stop copy operation
- Delete partially-restored files
- Show error message

### Corrupted Backup
**During Restore:**
- Validate backup exists and contains expected files
- Show error if validation fails
- Suggest user create new backup

### Network Paths
**Backup Location:**
- Warn user if backup path is on network drive
- Advise about slow backup/restore speeds

### Permission Errors
**File Operations:**
- Show detailed error message with affected file/folder
- Suggest running as administrator if needed
- Skip file and continue where possible

---

## Related Documentation

This feature integrates with:
- `Documentation/SESSION_CHANGES.md` - Will be updated with this feature
- `Documentation/PROJECT_SUMMARY.md` - Core architecture reference
- `README.md` - User-facing feature documentation

---

## Notes

- The Base Game Install profile serves as the **immutable reference point** for the application
- Users are **expected to clone** this profile to create their own mod profiles
- The backup is **single and immutable** to prevent accidental data loss
- **Restoration is destructive** (overwrites all game files) — multiple confirmations required
- This feature is **mandatory on first-run** to establish the clean game state baseline

---

**Document Status**: Ready for Implementation  
**Last Updated**: 2025-03-06  
**Version**: 1.0
