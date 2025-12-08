# Development Notes - XvT Hydrospanner

## Design Decisions

### Why WPF?
- Native Windows desktop application
- Rich UI capabilities
- Mature framework with extensive documentation
- Better performance than web-based alternatives for file operations
- No browser dependencies

### Why .NET 8.0?
- Latest LTS (Long Term Support) version
- Modern C# features (nullable reference types, pattern matching)
- Improved performance
- Cross-platform potential (though WPF is Windows-only)

### Why JSON for Storage?
- Human-readable for debugging
- Easy to edit manually if needed
- Native .NET support with Newtonsoft.Json
- Flexible schema evolution
- No database dependencies

### Service Layer Pattern
Separates business logic from UI:
- **ProfileManager**: Profile lifecycle
- **WarehouseManager**: File repository
- **ModApplicator**: File operations
- **ConfigurationManager**: Settings

Benefits:
- Testable business logic
- Reusable services
- Clear separation of concerns
- Easier to maintain

## Technical Challenges & Solutions

### Challenge 1: File Backup Strategy
**Problem**: How to safely backup original files before modification?

**Solution**:
- Create timestamped backups: `{mod-id}_{timestamp}_{filename}`
- Store in dedicated backup directory
- Keep configurable number of versions (default 5)
- Automatic cleanup of old backups

**Code Location**: `ModApplicator.CreateBackupAsync()`

### Challenge 2: Profile State Management
**Problem**: Track which profile is active and which mods are applied

**Solution**:
- `IsActive` flag on ModProfile
- `IsApplied` flag on FileModification
- Single active profile at a time
- Save state after each operation

**Code Location**: `ProfileManager.SetActiveProfileAsync()`

### Challenge 3: Warehouse File Uniqueness
**Problem**: Avoid filename conflicts in warehouse

**Solution**:
- Use GUID-based storage names: `{guid}{extension}`
- Maintain separate display name
- Store original filename in metadata
- Catalog tracks all mappings

**Code Location**: `WarehouseManager.AddFileAsync()`

### Challenge 4: Relative Path Handling
**Problem**: Game files use relative paths from installation root

**Solution**:
- Store only relative paths in modifications
- Combine with game install path at runtime
- Validate paths before operations
- Examples provided in UI

**Code Location**: `ModApplicator.ApplyModificationAsync()`

### Challenge 5: UI Responsiveness
**Problem**: File operations can block UI thread

**Solution**:
- Use async/await for all I/O operations
- Task-based operations
- UI remains responsive during file copying
- Progress feedback via status bar

**Example**: All service methods return `Task` or `Task<T>`

## Code Patterns

### Async/Await Pattern
```csharp
public async Task<ModProfile> CreateProfileAsync(string name, string description)
{
    var profile = new ModProfile { ... };
    await SaveProfileAsync(profile);
    return profile;
}
```

### Event Pattern for UI Updates
```csharp
public event EventHandler<ModProfile>? ProfileActivated;

ProfileActivated?.Invoke(this, profile);
```

### Null Safety with C# 8.0+
```csharp
public string? ActiveProfileId { get; set; }  // Nullable
public string Name { get; set; } = string.Empty;  // Non-nullable with default
```

### File Path Validation
```csharp
if (!Directory.Exists(path))
    throw new DirectoryNotFoundException($"Path not found: {path}");
```

## Data Structures

### Why List<T> over ObservableCollection<T>?
- Services use `List<T>` for data storage
- UI binds to `List<T>` (refreshed when needed)
- Simpler than change tracking
- Adequate for expected data sizes

**Note**: Could migrate to `ObservableCollection<T>` for real-time updates if needed.

### Why GUID for IDs?
- Globally unique, no collision risk
- No need for central ID registry
- Can generate client-side
- Suitable for distributed scenarios (future cloud sync)

### Dictionary for Custom Settings
```csharp
public Dictionary<string, string> CustomSettings { get; set; } = new();
```
- Extensible without schema changes
- Store arbitrary key-value pairs
- Future-proof for unknown requirements

## UI/UX Decisions

### Dark Theme
- Reduces eye strain for long sessions
- Matches modern IDE aesthetic
- Professional appearance
- Color scheme:
  - Background: #1E1E1E
  - Surface: #252526
  - Border: #3F3F46
  - Primary: #FFD700 (gold, Star Wars themed)
  - Accent: #0E639C (blue)
  - Danger: #A54040 (red)

### Navigation Pattern
- Left sidebar for main sections
- Frame-based content area
- Top bar for global controls
- Status bar for feedback

### Confirmation Dialogs
- Apply/Revert require confirmation (configurable)
- Delete operations always confirm
- Non-destructive operations don't confirm

### Search UX
- Real-time filtering (TextChanged event)
- Clear indication when filtered
- Simple text matching (case-insensitive)

## Testing Approach

### Manual Testing Focus Areas

#### 1. Profile Lifecycle
```
Create → Add Mods → Apply → Test in Game → Revert → Delete
```

#### 2. Warehouse Operations
```
Add File → Categorize → Search → Use in Profile → Delete
```

#### 3. File Operations
```
Apply Mod → Verify File → Check Backup → Revert → Verify Original
```

#### 4. Error Scenarios
- Missing game path
- Invalid file paths
- Permission denied
- Corrupted JSON
- Missing warehouse files

### Test Data Preparation
1. Create test profiles
2. Add sample files to warehouse
3. Apply modifications
4. Verify game launches
5. Test revert functionality

## Performance Considerations

### File Operations
- **Async I/O**: All file operations use async methods
- **No blocking**: UI remains responsive
- **Batch operations**: Process multiple files efficiently

### Search Performance
- **In-memory search**: Fast for typical warehouse sizes
- **Simple string matching**: O(n) complexity acceptable
- **Future**: Could add indexing for large warehouses

### Memory Usage
- **JSON caching**: Catalogs loaded once, cached in memory
- **Lazy loading**: Could implement for very large warehouses
- **File streaming**: Large files not held in memory

## Error Handling Strategy

### Service Layer
- Throw exceptions for exceptional cases
- Return false/null for expected failures
- Log errors to console (could add file logging)

### UI Layer
- Catch exceptions from services
- Show user-friendly MessageBox
- Continue operation when possible
- Graceful degradation

### Example Pattern
```csharp
try
{
    await _modApplicator.ApplyModificationAsync(mod);
    StatusText.Text = "Applied successfully";
}
catch (Exception ex)
{
    MessageBox.Show($"Error applying mod: {ex.Message}", 
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
}
```

## Configuration Management

### Default Paths
```csharp
var appDataPath = Path.Combine(
    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
    "XvTHydrospanner"
);
```

### Path Validation
```csharp
public (bool isValid, List<string> errors) ValidateConfig()
{
    var errors = new List<string>();
    if (!Directory.Exists(_config.GameInstallPath))
        errors.Add("Game path not found");
    return (errors.Count == 0, errors);
}
```

## Future Optimization Ideas

### 1. Parallel File Operations
```csharp
// Apply multiple mods in parallel
var tasks = modifications.Select(m => ApplyModificationAsync(m));
await Task.WhenAll(tasks);
```

### 2. File Hash Verification
```csharp
// Verify file integrity
var hash = ComputeSHA256(filePath);
if (hash != expectedHash)
    throw new InvalidDataException("File corrupted");
```

### 3. Incremental Backup
- Only backup if file changed
- Compare timestamps or hashes
- Reduces redundant backups

### 4. Warehouse Indexing
```csharp
// Build search index
private Dictionary<string, List<WarehouseFile>> _searchIndex;

private void BuildSearchIndex()
{
    _searchIndex = _catalog
        .SelectMany(f => f.Tags.Select(t => new { Tag = t, File = f }))
        .GroupBy(x => x.Tag.ToLower())
        .ToDictionary(g => g.Key, g => g.Select(x => x.File).ToList());
}
```

## Known Issues & Workarounds

### Issue 1: FolderBrowserDialog is WinForms
**Workaround**: Reference System.Windows.Forms
```xml
<UseWindowsForms>true</UseWindowsForms>
```

**Future**: Replace with native WPF folder browser or third-party control

### Issue 2: No TreeView Auto-Expand
**Workaround**: Users manually expand directories
**Future**: Implement auto-expand for key directories

### Issue 3: Large Warehouse Performance
**Current**: All files loaded into memory
**Future**: Implement paging or virtualization if warehouse grows large

## Development Environment

### Recommended Tools
- **IDE**: Visual Studio 2022 (Community or higher)
- **Extensions**: 
  - XAML Styler (formatting)
  - ReSharper (optional, refactoring)
- **Version Control**: Git

### Project Settings
- **C# Language Version**: Latest
- **Nullable Reference Types**: Enabled
- **Warning Level**: 4
- **Treat Warnings as Errors**: Recommended for production

### Debugging Tips
1. **JSON Files**: Check AppData folder for corruption
2. **File Operations**: Add breakpoints in ModApplicator
3. **UI Issues**: Use XAML Hot Reload
4. **Service Issues**: Add console logging

## Deployment Checklist

### Pre-Release
- [ ] Test on clean Windows install
- [ ] Verify all features work
- [ ] Check error handling
- [ ] Test with actual game installation
- [ ] Validate backup/restore functionality

### Release Build
- [ ] Build in Release configuration
- [ ] Remove debug symbols
- [ ] Test executable independently
- [ ] Create installer (optional)
- [ ] Package with README and QUICKSTART

### Documentation
- [ ] Update README with version info
- [ ] Document known issues
- [ ] Include sample profiles (optional)
- [ ] Add troubleshooting guide

## Maintenance Tasks

### Regular Maintenance
1. **Dependency Updates**: Check for package updates quarterly
2. **Security Patches**: Apply .NET security updates
3. **Bug Fixes**: Address user-reported issues
4. **Documentation**: Keep docs in sync with code

### Version Numbering
- **Major**: Breaking changes, architecture changes
- **Minor**: New features, backward compatible
- **Patch**: Bug fixes only

Example: 1.0.0 → 1.1.0 (new feature) → 1.1.1 (bug fix)

## Community Contributions

### Contribution Guidelines
1. Fork repository
2. Create feature branch
3. Follow existing code style
4. Add documentation for new features
5. Test thoroughly
6. Submit pull request

### Code Review Focus
- Functionality correctness
- Error handling
- Code clarity
- Documentation
- Performance implications

## Resources & References

### WPF Learning
- Microsoft WPF Documentation
- WPF Tutorial (wpf-tutorial.com)
- WPF Samples (github.com/microsoft/WPF-Samples)

### C# Best Practices
- C# Coding Conventions (Microsoft)
- Async/Await Best Practices
- SOLID Principles

### Star Wars XvT Community
- GOG Forums
- ModDB
- Steam Community

---

**Notes compiled by**: AI Assistant
**Date**: December 3, 2025
**Project**: XvT Hydrospanner v1.0
