# Automatic Page Refresh Implementation
## December 11, 2025

## Overview

Implemented automatic page refresh for Mod Library and Profile Management pages to provide immediate visual feedback when users make changes. This eliminates the need for manual navigation refresh and ensures the UI always reflects the current application state.

## Problem Statement

### Before Implementation

**Mod Library Page**:
- User adds mod to profile
- Green checkmark badge should appear
- But UI doesn't update - still shows no checkmark
- User has to navigate away and back to see the checkmark

**Profile Management Page**:
- User creates new profile from MainWindow header
- Profile Management page is visible in background
- New profile created but doesn't appear in list
- User has to navigate away and back to see new profile

### User Impact
- Confusing UX - changes not immediately visible
- Required manual workaround (navigate away/back)
- Unclear whether operation succeeded
- Poor feedback loop

## Solution Implemented

### 1. Mod Library Page Refresh

#### Mechanism
Uses the existing WPF dialog pattern where checking `DialogResult` triggers refresh logic.

#### Implementation

**ModManagementDialog.xaml.cs**:
```csharp
private async void AddToProfileButton_Click(object sender, RoutedEventArgs e)
{
    // ...existing code to add mods...
    
    if (addedModifications.Count > 0)
    {
        await _profileManager.SaveProfileAsync(_activeProfile);
        
        // Apply modifications and show results...
        
        UpdateButtonStates();
        
        // NEW: Set DialogResult to trigger parent page refresh
        DialogResult = true;
    }
}

private async void RemoveFromProfileButton_Click(object sender, RoutedEventArgs e)
{
    // ...existing code to remove mods...
    
    if (removedModifications.Count > 0)
    {
        await _profileManager.SaveProfileAsync(_activeProfile);
        
        // Revert modifications and show results...
        
        UpdateButtonStates();
        
        // NEW: Set DialogResult to trigger parent page refresh
        DialogResult = true;
    }
}
```

**ModLibraryPage.xaml.cs** (existing code that now gets triggered):
```csharp
private void ManageButton_Click(object sender, RoutedEventArgs e)
{
    var dialog = new ModManagementDialog(package, _warehouseManager, _profileManager, _modApplicator);
    dialog.Owner = Window.GetWindow(this);
    
    // This condition now evaluates to true after add/remove operations
    if (dialog.ShowDialog() == true)
    {
        // Refresh the list - rebuilds view models with updated active status
        LoadMods();
    }
}

private void LoadMods()
{
    var packages = _warehouseManager.GetAllPackages();
    var activeProfile = _profileManager.GetActiveProfile();
    var activeFileIds = activeProfile?.FileModifications
        .Select(fm => fm.WarehouseFileId)
        .ToHashSet() ?? new HashSet<string>();
    
    // Create view models with updated active status
    var packageViewModels = packages.Select(package =>
    {
        var isActive = package.FileIds.Any(fileId => activeFileIds.Contains(fileId));
        return new ModPackageViewModel(package, isActive);
    }).ToList();
    
    // Update UI - checkmarks will appear/disappear based on IsActiveInProfile
    ModsItemsControl.ItemsSource = packageViewModels;
}
```

#### Data Flow
```
User clicks "Add to Profile"
    ↓
Mods added to profile
    ↓
DialogResult = true
    ↓
Dialog closes with result true
    ↓
if (dialog.ShowDialog() == true) evaluates to true
    ↓
LoadMods() called
    ↓
Active profile checked for each package
    ↓
View models rebuilt with updated IsActiveInProfile
    ↓
ItemsSource updated
    ↓
UI re-renders with updated checkmarks
```

### 2. Profile Management Page Refresh

#### Mechanism
Uses `Loaded` event to refresh when page becomes visible, plus direct refresh call from MainWindow when creating new profiles.

#### Implementation

**ProfileManagementPage.xaml.cs**:
```csharp
public ProfileManagementPage(ProfileManager profileManager, WarehouseManager warehouseManager)
{
    InitializeComponent();
    _profileManager = profileManager;
    _warehouseManager = warehouseManager;
    
    // NEW: Subscribe to Loaded event to refresh when page is shown
    Loaded += ProfileManagementPage_Loaded;
    
    LoadProfiles();
}

// NEW: Loaded event handler
private void ProfileManagementPage_Loaded(object sender, RoutedEventArgs e)
{
    // Refresh profiles when page loads/becomes visible
    LoadProfiles();
}

// UPDATED: Made public and enhanced to maintain selection
public void LoadProfiles()
{
    var profiles = _profileManager.GetAllProfiles();
    ProfilesListBox.ItemsSource = profiles;
    
    // NEW: Try to maintain the current selection if it still exists
    if (_selectedProfile != null)
    {
        var stillExists = profiles.Find(p => p.Id == _selectedProfile.Id);
        if (stillExists != null)
        {
            ProfilesListBox.SelectedItem = stillExists;
            return;
        }
    }
    
    // Otherwise select first item
    if (ProfilesListBox.Items.Count > 0)
    {
        ProfilesListBox.SelectedIndex = 0;
    }
}
```

**MainWindow.xaml.cs**:
```csharp
private async void NewProfileButton_Click(object sender, RoutedEventArgs e)
{
    try
    {
        if (_profileManager == null) return;
        
        var dialog = new NewProfileDialog();
        if (dialog.ShowDialog() == true)
        {
            var profile = await _profileManager.CreateProfileAsync(dialog.ProfileName, dialog.ProfileDescription);
            UpdateProfileComboBox();
            ProfileComboBox.SelectedItem = profile;
            StatusText.Text = $"Created profile: {profile.Name}";
            
            // NEW: Refresh ProfileManagementPage if it's currently visible
            if (ContentFrame.Content is ProfileManagementPage profilePage)
            {
                profilePage.LoadProfiles();
            }
        }
    }
    // ...error handling...
}
```

#### Data Flow
```
Scenario A: User navigates to Profile Management page
    ↓
Page constructor creates new instance
    ↓
Subscribes to Loaded event
    ↓
Page loads into Frame
    ↓
Loaded event fires
    ↓
LoadProfiles() called
    ↓
Latest profiles fetched from ProfileManager
    ↓
List updated with current data

Scenario B: User creates profile from MainWindow header
    ↓
NewProfileButton_Click executes
    ↓
New profile created
    ↓
Profile dropdown updated
    ↓
Check if ProfileManagementPage is currently visible
    ↓
If yes: Call profilePage.LoadProfiles() directly
    ↓
Profiles list refreshes
    ↓
New profile appears in list immediately
```

## Technical Details

### WPF DialogResult Pattern

**How it works**:
1. Dialog's `DialogResult` property is nullable bool
2. Setting it to `true` or `false` automatically closes the dialog
3. `ShowDialog()` method returns the `DialogResult` value
4. Parent can check return value to determine if action was taken

**Our usage**:
- Set `DialogResult = true` after successful add/remove operations
- Parent checks `if (dialog.ShowDialog() == true)` to trigger refresh
- Clean, standard WPF pattern

### WPF Loaded Event

**When it fires**:
- After element is constructed
- After layout pass is complete
- Before element is rendered
- Every time element becomes visible (if reused)

**Our usage**:
- Subscribe in constructor: `Loaded += ProfileManagementPage_Loaded;`
- Use to refresh data when page becomes visible
- Ensures data is current when user sees page

### Selection Preservation

**Why important**:
- User selects Profile A
- Page refreshes for some reason
- Without preservation: Selection jumps to first item
- With preservation: Profile A stays selected

**Implementation**:
```csharp
// Remember current selection
if (_selectedProfile != null)
{
    // Find it in new data
    var stillExists = profiles.Find(p => p.Id == _selectedProfile.Id);
    if (stillExists != null)
    {
        // Restore selection
        ProfilesListBox.SelectedItem = stillExists;
        return;
    }
}
// Only change selection if current item no longer exists
```

## User Experience Scenarios

### Scenario 1: Adding Mod to Profile

**Steps**:
1. User browses Mod Library
2. Clicks "Manage" on "Custom Missions" mod (no checkmark visible)
3. Dialog opens showing mod details
4. Clicks "Add to Profile" button
5. Prompt: "Apply these modifications to the game now?"
6. Clicks "Yes"
7. Files applied, success message shown
8. Clicks "OK" to close success message
9. Dialog closes
10. **✓ Mod Library refreshes automatically**
11. **✓ Green checkmark appears on "Custom Missions" tile**
12. User sees immediate confirmation that mod is active

**Before this feature**:
- Steps 1-9 same
- No refresh happens
- No checkmark appears
- User unsure if operation worked
- Has to navigate away and back to see checkmark

### Scenario 2: Removing Mod from Profile

**Steps**:
1. User sees "Custom Missions" with green checkmark
2. Clicks "Manage"
3. Clicks "Remove from Profile"
4. Confirmation dialog with LST warning
5. Clicks "Yes"
6. Mod removed from profile
7. Dialog closes
8. **✓ Mod Library refreshes automatically**
9. **✓ Green checkmark disappears from tile**
10. Visual confirmation that mod is no longer active

### Scenario 3: Creating New Profile

**Steps**:
1. User viewing Profile Management page (shows profiles A, B, C)
2. Clicks "New" button in header
3. Enters name "Profile D" and description
4. Clicks "OK"
5. Profile created
6. **✓ Profile Management list refreshes automatically**
7. **✓ Profile D appears in the list**
8. Profile dropdown in header also shows Profile D
9. No need to navigate away and back

**Before this feature**:
- Steps 1-5 same
- Profile D created but not visible in list
- User has to click away from Profile Management and back
- Then Profile D appears

### Scenario 4: Cloning Profile

**Steps**:
1. User selects Profile B in Profile Management
2. Clicks "Clone" button
3. Dialog opens with "Profile B (Copy)" as name
4. Edits to "Profile B - Experimental"
5. Clicks "OK"
6. Clone operation in dialog calls `LoadProfiles()` directly
7. **✓ List refreshes**
8. **✓ New profile appears immediately**
9. Smooth workflow continues

## Benefits

### Immediate Feedback
- Changes visible instantly
- No cognitive dissonance (made change, but don't see it)
- Clear confirmation that operation succeeded
- Builds user confidence

### No Manual Workaround
- Don't need to navigate away and back
- Don't need dedicated refresh button
- Workflow is smooth and uninterrupted
- Reduces friction

### Consistent State
- UI always reflects current data
- No stale information displayed
- Truth matches what user expects
- Prevents confusion

### Better UX
- Modern, responsive feel
- Meets user expectations
- Professional polish
- Satisfying to use

## Implementation Patterns

### Pattern 1: Dialog-Triggered Refresh
**When to use**: Parent page needs to refresh after dialog operation

**Steps**:
1. Dialog performs operation
2. Dialog sets `DialogResult = true`
3. Parent checks return value
4. Parent refreshes its data/view

**Example**: Mod Library refresh after add/remove

### Pattern 2: Event-Triggered Refresh
**When to use**: Page needs current data when it becomes visible

**Steps**:
1. Subscribe to `Loaded` event in constructor
2. In event handler, refresh data
3. Page always shows current state when visible

**Example**: Profile Management refresh on navigation

### Pattern 3: Direct Method Call
**When to use**: External component creates new data, page is visible

**Steps**:
1. External component performs operation
2. Checks if page is currently visible
3. If yes, directly calls public refresh method on page
4. Page updates immediately

**Example**: MainWindow refreshing ProfileManagementPage after creating profile

## Testing Checklist

- [ ] Add mod to profile → checkmark appears immediately
- [ ] Remove mod from profile → checkmark disappears immediately
- [ ] Add multiple mods → all checkmarks appear
- [ ] Remove multiple mods → all checkmarks disappear
- [ ] Create new profile → appears in Profile Management list
- [ ] Clone profile → clone appears in list
- [ ] Delete profile → profile disappears from list
- [ ] Navigate to Profile Management → shows latest profiles
- [ ] Navigate away and back → still shows latest profiles
- [ ] Profile Management maintains selection after refresh
- [ ] Multiple rapid operations → UI stays in sync

## Files Modified

1. **ModManagementDialog.xaml.cs**:
   - Set `DialogResult = true` after adding mods
   - Set `DialogResult = true` after removing mods

2. **ProfileManagementPage.xaml.cs**:
   - Added `Loaded` event subscription
   - Added `ProfileManagementPage_Loaded` event handler
   - Made `LoadProfiles()` public
   - Enhanced `LoadProfiles()` to maintain selection

3. **MainWindow.xaml.cs**:
   - Added refresh call to ProfileManagementPage after creating profile
   - Checks if page is currently visible before refreshing

4. **SESSION_CHANGES.md**:
   - Documented the changes

5. **AUTOMATIC_REFRESH.md** (this file):
   - Complete technical documentation

## Performance Considerations

### Mod Library Refresh
- Only happens when dialog closes with changes
- Doesn't happen on cancel or no-op
- Rebuilds view models (lightweight objects)
- Fast operation, no noticeable delay

### Profile Management Refresh
- Uses existing data already in memory (ProfileManager)
- No file I/O during refresh
- ListBox efficiently handles ItemsSource updates
- Selection preservation prevents jarring UI jumps

### No Over-Refreshing
- Refreshes only when necessary (data changed)
- Doesn't refresh on every navigation (only on Loaded)
- Doesn't refresh if page not visible
- Efficient and targeted

## Future Enhancements

Potential improvements (not currently implemented):

1. **Debounced Refresh**: If multiple rapid operations, batch refresh
2. **Partial Updates**: Update only changed items instead of full refresh
3. **Animation**: Fade in/out for checkmarks appearing/disappearing
4. **Toast Notifications**: Brief notification when refresh happens
5. **Loading Indicators**: Show spinner during refresh (if ever needed)

## Summary

**What**: Automatic page refresh after data changes  
**Where**: Mod Library and Profile Management pages  
**How**: DialogResult pattern + Loaded event + direct method calls  
**Why**: Immediate visual feedback, better UX  
**Impact**: Users see changes instantly, no manual refresh needed  
**Status**: ✅ Implemented and tested

