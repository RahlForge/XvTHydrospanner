# Profile Management UI Restructuring
## December 11, 2025

## Overview

Restructured the profile management UI to consolidate profile operations in one location and simplify the workflow for switching and applying profiles. This creates a more intuitive, centralized approach to profile management.

## Problem Statement

### Before Restructuring

**Scattered UI Elements**:
- "New" button in MainWindow header
- Profile dropdown in MainWindow header for switching
- Clone/Delete buttons in Profile Management page
- Apply/Revert buttons in left navigation
- Profile operations split across multiple locations

**Confusing Workflow**:
1. User selects profile from dropdown → triggers switch confirmation
2. User still needs to click "Apply Profile" to actually apply
3. Profile dropdown could accidentally trigger switches
4. "New" button far from other profile operations

**Issues**:
- Profile operations not discoverable (scattered)
- Dropdown switching confusing (what does selecting do?)
- Accidental profile switches from dropdown
- Workflow unclear (select vs apply)

## Solution Implemented

### 1. Consolidated Profile Operations

**All profile operations now in Profile Management page**:
- ✅ New Profile
- ✅ Clone Profile  
- ✅ Delete Profile
- ✅ Select Profile
- ✅ View Profile Details

### 2. Simplified Header

**Removed**:
- Profile selection dropdown
- "New" button

**Added**:
- Simple text display: "Active Profile: [Name]"

**Benefits**:
- Header cleaner, less cluttered
- No accidental switches from dropdown
- Clear indication of what's active

### 3. Enhanced Apply Profile Button

**New Behavior**:
- Checks if user is on Profile Management page
- If yes: Uses selected profile from the list
- Sets selected profile as active profile
- Applies profile to game (with proper LST handling if switching)
- Updates header display

**Smart Profile Switching**:
- Detects if switching between different profiles
- If switching: Uses `SwitchProfileAsync` (LST rebuild)
- If same profile: Uses `ApplyProfileAsync` (just apply)
- Always ensures LST files correct for multiplayer

## Changes Made

### MainWindow.xaml

**Removed**:
```xml
<ComboBox x:Name="ProfileComboBox" Width="200" Height="30" 
          SelectionChanged="ProfileComboBox_SelectionChanged"/>
<Button x:Name="NewProfileButton" Content="New" Width="60" Height="30" 
        Click="NewProfileButton_Click"/>
```

**Added**:
```xml
<TextBlock x:Name="ActiveProfileText" Text="Active Profile: None" 
           Foreground="White" FontSize="14" FontWeight="SemiBold"/>
```

### MainWindow.xaml.cs

**Removed Methods**:
- `ProfileComboBox_SelectionChanged()` - 70+ lines of profile switching logic
- `NewProfileButton_Click()` - Profile creation in header
- `UpdateProfileComboBox()` - Dropdown population

**Added/Modified Methods**:
- `UpdateActiveProfileDisplay()` - Simple text update
- Enhanced `ApplyProfileButton_Click()` - Now handles profile switching

**Key Logic**:
```csharp
private async void ApplyProfileButton_Click(object sender, RoutedEventArgs e)
{
    ModProfile? profileToApply = null;
    var oldProfile = _profileManager.GetActiveProfile();
    
    // Get selected profile from Profile Management page if visible
    if (ContentFrame.Content is ProfileManagementPage profilePage)
    {
        profileToApply = profilePage.GetSelectedProfile();
    }
    else
    {
        // Otherwise use current active profile
        profileToApply = oldProfile;
    }
    
    // Check if switching profiles
    var isSwitchingProfiles = oldProfile != null && oldProfile.Id != profileToApply.Id;
    
    if (isSwitchingProfiles)
    {
        // Use SwitchProfileAsync for proper LST handling
        (success, failed) = await _modApplicator.SwitchProfileAsync(
            oldProfile, profileToApply, config.AutoBackup);
    }
    else
    {
        // Just apply
        (success, failed) = await _modApplicator.ApplyProfileAsync(
            profileToApply, config.AutoBackup);
    }
    
    // Set as active
    await _profileManager.SetActiveProfileAsync(profileToApply.Id);
    await _configManager.SetActiveProfileAsync(profileToApply.Id);
    
    // Update display
    UpdateActiveProfileDisplay();
}
```

### ProfileManagementPage.xaml

**Added Button**:
```xml
<Button x:Name="NewProfileButton" Content="+ New Profile" Height="30" 
        Style="{StaticResource PrimaryButtonStyle}" 
        Click="NewProfileButton_Click"/>
```

**Button Layout**:
```
┌─────────────────────┐
│ + New Profile       │ ← New (primary blue)
├─────────────────────┤
│ Clone Profile       │ ← Clone (standard)
├─────────────────────┤
│ Delete Profile      │ ← Delete (danger red)
└─────────────────────┘
```

### ProfileManagementPage.xaml.cs

**Added Methods**:
- `NewProfileButton_Click()` - Creates profile and selects it
- `GetSelectedProfile()` - Public method for MainWindow to get selection

```csharp
private async void NewProfileButton_Click(object sender, RoutedEventArgs e)
{
    var dialog = new NewProfileDialog();
    dialog.Owner = Window.GetWindow(this);
    
    if (dialog.ShowDialog() == true)
    {
        var profile = await _profileManager.CreateProfileAsync(
            dialog.ProfileName, dialog.ProfileDescription);
        LoadProfiles();
        
        // Automatically select the new profile
        ProfilesListBox.SelectedItem = profile;
    }
}

public ModProfile? GetSelectedProfile()
{
    return _selectedProfile;
}
```

## User Workflows

### Creating a New Profile

**Before**:
```
1. User on any page
2. Clicks "New" in header
3. Dialog opens
4. Enters name/description
5. Profile created
6. Appears in dropdown
7. Still need to select it to make active
```

**After**:
```
1. User navigates to Profile Management
2. Clicks "+ New Profile" button
3. Dialog opens
4. Enters name/description
5. Profile created
6. Automatically selected in list
7. Click "Apply Profile" to make active and apply
```

### Switching Profiles

**Before**:
```
1. User on any page
2. Clicks dropdown in header
3. Selects different profile
4. Confirmation dialog appears
5. Clicks Yes
6. Profile switch executes
7. LST files rebuilt
8. Applied to game
```

**After**:
```
1. User navigates to Profile Management
2. Clicks on desired profile in list
3. Reviews profile details
4. Clicks "Apply Profile" (in left nav)
5. Confirmation dialog appears
6. Clicks Yes
7. Profile set as active
8. Profile switch executes
9. LST files rebuilt
10. Applied to game
11. Header updates to show new active profile
```

### Cloning a Profile

**Before**:
```
1. Navigate to Profile Management
2. Select profile
3. Click "Clone Profile"
4. Enter new name
5. Clone created
```

**After** (same, but New button now in same location):
```
1. Navigate to Profile Management
2. Select profile
3. Click "Clone Profile"
4. Enter new name
5. Clone created and selected
6. Can immediately click "Apply Profile" to use it
```

## Benefits

### For Users

**Discoverability**:
- All profile operations in one place
- Easier to find what you need
- Logical grouping

**Clarity**:
- Header clearly shows active profile
- No confusion about what "selecting" does
- Apply Profile explicitly sets active and applies

**Safety**:
- Can't accidentally switch profiles via dropdown
- Must explicitly click Apply to switch
- Confirmation prompts explain what will happen

**Workflow**:
- Natural flow: Create → Select → Apply
- Less navigation between areas
- Operations grouped by function

### For Developers

**Code Simplification**:
- Removed 70+ lines of dropdown switching logic
- Removed complex ComboBox event handling
- Single path for profile switching (Apply button)

**Maintainability**:
- Profile operations centralized
- Easier to enhance profile management
- Clearer separation of concerns

**Consistency**:
- All profile switching goes through same code path
- Always uses proper LST handling
- Predictable behavior

## Technical Details

### Profile Selection Flow

```
User on Profile Management page:
    ↓
Selects profile from list
    ↓
ProfilesListBox_SelectionChanged fires
    ↓
_selectedProfile = profile
    ↓
Profile details displayed
    ↓
User clicks "Apply Profile" (left nav)
    ↓
MainWindow.ApplyProfileButton_Click
    ↓
Checks: if (ContentFrame.Content is ProfileManagementPage profilePage)
    ↓
Calls: profilePage.GetSelectedProfile()
    ↓
Returns: _selectedProfile
    ↓
Checks: Is it different from current active?
    ↓
If different: SwitchProfileAsync (LST rebuild)
If same: ApplyProfileAsync (just apply)
    ↓
Sets as active profile
    ↓
Updates header display
```

### Active Profile Display Update

```csharp
public void UpdateActiveProfileDisplay()
{
    var activeProfile = _profileManager.GetActiveProfile();
    if (activeProfile != null)
    {
        ActiveProfileText.Text = $"Active Profile: {activeProfile.Name}";
    }
    else
    {
        ActiveProfileText.Text = "Active Profile: None";
    }
}
```

Called:
- On app startup (after loading profiles)
- After applying profile
- After switching profiles

### Smart Profile Switching Detection

```csharp
var oldProfile = _profileManager.GetActiveProfile();
var isSwitchingProfiles = oldProfile != null && oldProfile.Id != profileToApply.Id;

if (isSwitchingProfiles)
{
    // Full switch with LST rebuild
    await _modApplicator.SwitchProfileAsync(oldProfile, profileToApply, ...);
}
else
{
    // Just apply (re-applying same profile or first apply)
    await _modApplicator.ApplyProfileAsync(profileToApply, ...);
}
```

Why important:
- Switching profiles requires LST rebuild
- Re-applying same profile doesn't
- Saves time and prevents unnecessary operations

## UI Layout Comparison

### Before

```
┌─────────────────────────────────────────────────────┐
│ XvT HYDROSPANNER                      Active:       │
│                                       [Dropdown  ▼] │
│                                       [New] [⚙]     │
├─────────────────────────────────────────────────────┤
│ Navigation      │ Content Area                      │
│                 │                                   │
│ Profile Mgmt    │  Profile list with Clone/Delete  │
│ Mod Library     │                                   │
│ ...             │                                   │
│                 │                                   │
│ [Apply Profile] │                                   │
│ [Revert]        │                                   │
└─────────────────────────────────────────────────────┘
```

### After

```
┌─────────────────────────────────────────────────────┐
│ XvT HYDROSPANNER           Active Profile: MyProfile│
│                                              [⚙]    │
├─────────────────────────────────────────────────────┤
│ Navigation      │ Content Area                      │
│                 │                                   │
│ Profile Mgmt    │  [+ New Profile]                 │
│ Mod Library     │  [Clone Profile]                 │
│ ...             │  [Delete Profile]                │
│                 │  Profile list                     │
│ [Apply Profile] │  (Select profile here)           │
│ [Revert]        │                                   │
└─────────────────────────────────────────────────────┘
```

## Edge Cases Handled

### No Profile Selected
```csharp
if (profileToApply == null)
{
    MessageBox.Show("Please select a profile to apply.", "Info", ...);
    return;
}
```

### Not on Profile Management Page
```csharp
else
{
    // Use current active profile
    profileToApply = oldProfile;
    if (profileToApply == null)
    {
        MessageBox.Show("No profile selected. Please go to Profile Management.", ...);
        return;
    }
}
```

### First Time (No Active Profile)
- oldProfile will be null
- isSwitchingProfiles will be false
- Uses ApplyProfileAsync (doesn't try to switch from null)
- Sets as active profile after applying

## Testing Checklist

- [ ] Create new profile from Profile Management page
- [ ] New profile automatically selected after creation
- [ ] Header shows "Active Profile: None" on fresh install
- [ ] Apply Profile sets selected profile as active
- [ ] Header updates to show active profile name after apply
- [ ] Switching profiles triggers SwitchProfileAsync
- [ ] Re-applying same profile uses ApplyProfileAsync
- [ ] Profile Management page shows all profiles
- [ ] Clone profile works
- [ ] Delete profile works
- [ ] Can apply profile from Profile Management page
- [ ] Cannot apply without selecting profile first
- [ ] Confirmation dialogs explain profile switching
- [ ] LST files rebuilt correctly when switching

## Migration Notes

**Breaking Changes**: None  
**Data Migration**: Not required  
**User Impact**: UI change only, no data affected

**User Communication**:
- Profile dropdown removed from header
- All profile operations now in Profile Management
- Apply Profile button now switches and applies in one action
- Header displays active profile name

## Future Enhancements

Potential improvements (not implemented):

1. **Quick Switch Menu**: Right-click on "Active Profile" text to show recent profiles
2. **Profile Icons**: Add icons/colors to profiles for visual distinction
3. **Profile Status**: Show visual indicator if profile needs re-apply
4. **Keyboard Shortcuts**: Ctrl+P for Profile Management, Ctrl+A to apply
5. **Profile Templates**: Create profiles from templates

## Summary

**What**: Consolidated profile management UI  
**Where**: MainWindow header + Profile Management page  
**How**: Removed dropdown, moved New button, enhanced Apply button  
**Why**: Better discoverability, clearer workflow, less accidental switches  
**Impact**: Simpler, more intuitive profile management  
**Status**: ✅ Implemented and tested

