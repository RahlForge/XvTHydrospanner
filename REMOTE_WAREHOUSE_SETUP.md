# Remote Warehouse Setup Guide

This guide explains how to set up a central GitHub repository to host mods for XvT Hydrospanner.

## Repository Structure

Create a GitHub repository with the following structure:

```
XvTHydrospanner-Mods/
├── catalog.json          # Main catalog file listing all available mods
├── files/               # Individual mod files
│   ├── BATTLE01.TIE
│   ├── TRAIN05.TIE
│   └── ...
└── packages/            # Mod package archives
    ├── MissionPack.zip
    └── ...
```

## Catalog Format

The `catalog.json` file must be in the root of the repository and follow this format:

```json
{
  "Version": "1.0",
  "RepositoryUrl": "https://github.com/YourUsername/XvTHydrospanner-Mods",
  "Files": [
    {
      "Id": "unique-mod-id",
      "Name": "Mod Display Name",
      "Description": "Description of what this mod does",
      "OriginalFileName": "BATTLE01.TIE",
      "FileExtension": ".TIE",
      "Category": "Battle",
      "TargetRelativePath": "BalanceOfPower/BATTLE/BATTLE01.TIE",
      "FileSizeBytes": 52480,
      "DateAdded": "2025-12-08T00:00:00Z",
      "Author": "Modder Name",
      "Version": "1.0",
      "Tags": ["battle", "enhanced", "ai"],
      "DownloadUrl": "https://raw.githubusercontent.com/YourUsername/XvTHydrospanner-Mods/main/files/BATTLE01.TIE",
      "Sha": "optional-sha-hash"
    }
  ],
  "Packages": [
    {
      "Id": "unique-package-id",
      "Name": "Package Display Name",
      "Description": "Description of the package",
      "Author": "Package Author",
      "Version": "2.0",
      "DateAdded": "2025-12-08T00:00:00Z",
      "Tags": ["missions", "complete"],
      "FileIds": [],
      "DownloadUrl": "https://raw.githubusercontent.com/YourUsername/XvTHydrospanner-Mods/main/packages/MissionPack.zip",
      "Sha": "optional-sha-hash"
    }
  ]
}
```

## Field Descriptions

### For Files:

- **Id**: Unique identifier (GUID recommended)
- **Name**: Display name shown in the app
- **Description**: What the mod does
- **OriginalFileName**: The original file name
- **FileExtension**: Extension including the dot (e.g., ".TIE")
- **Category**: One of: Battle, Mission, Training, Melee, Campaign, Tournament, Graphics, Music, Sound, Resource, Configuration, Other
- **TargetRelativePath**: Where the file should be installed relative to game root
- **FileSizeBytes**: File size in bytes
- **DateAdded**: ISO 8601 date string
- **Author**: Mod creator (optional)
- **Version**: Version string (optional)
- **Tags**: Array of search tags
- **DownloadUrl**: Raw GitHub URL to the file
- **Sha**: SHA hash for verification (optional)

### For Packages:

- **Id**: Unique identifier
- **Name**: Package display name
- **Description**: What's included in the package
- **Author**: Package creator (optional)
- **Version**: Version string (optional)
- **DateAdded**: ISO 8601 date string
- **Tags**: Array of search tags
- **FileIds**: Leave empty (populated when extracted)
- **DownloadUrl**: Raw GitHub URL to the ZIP archive
- **Sha**: SHA hash for verification (optional)

## Download URLs

For files in the repository, use the raw GitHub URL format:
```
https://raw.githubusercontent.com/USERNAME/REPO/BRANCH/path/to/file.ext
```

Example:
```
https://raw.githubusercontent.com/RahlForge/XvTHydrospanner-Mods/main/files/BATTLE01.TIE
```

## Configuration in XvT Hydrospanner

By default, the app looks for:
- **Owner**: RahlForge
- **Repository**: XvTHydrospanner-Mods
- **Branch**: main

You can customize these in Settings (⚙ button) > Remote Repository Settings.

## GitHub Personal Access Token

To upload mods from the app, you need a GitHub Personal Access Token:

1. Go to https://github.com/settings/tokens
2. Click "Generate new token" → "Generate new token (classic)"
3. Give it a descriptive name (e.g., "XvT Hydrospanner Upload")
4. Select scopes:
   - ✅ `repo` (Full control of private repositories)
5. Click "Generate token"
6. Copy the token (you won't see it again!)
7. In XvT Hydrospanner: Settings → Remote Repository Settings → paste token
8. Click Save

**Security Note**: The token is stored in your local config.json file. Keep this secure!

## Uploading Mods from the App

The easiest way to add mods to your remote repository:

1. **Import Locally**: Add mod to local warehouse via "Mod Warehouse" page
2. **Test**: Create a profile and test that the mod applies correctly
3. **Upload**: 
   - Go to "Mod Warehouse" page
   - Select the mod file
   - Click "⬆ Upload to Remote"
   - Confirm the upload

The app will:
- Upload the file to `files/{id}.ext` in your repository
- Automatically update or create `catalog.json` with the mod entry
- Preserve all metadata (name, description, version, tags, etc.)

## Adding New Mods Manually

If you prefer manual control:

1. Add the mod file to the appropriate directory (files/ or packages/)
2. Update catalog.json with the new entry
3. Commit and push to GitHub
4. Users can refresh their remote catalog in the app to see new mods

## Best Practices

- Keep file names consistent with original game naming conventions
- Test mods before adding them to the catalog
- Include clear descriptions and tags for searchability
- Use semantic versioning (e.g., 1.0, 1.1, 2.0)
- Update the catalog version if you make breaking changes to the format
- Consider using Git LFS for large files (>50MB)

## Example Repository

See `sample-catalog.json` for a working example of the catalog format.
