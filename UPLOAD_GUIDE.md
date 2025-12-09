# Remote Warehouse Upload Feature - Quick Start Guide

## Overview
You can now upload mods from your local warehouse directly to a GitHub repository, making them available for other users to download.

## Setup (One-Time)

### 1. Create GitHub Repository
```
Repository name: XvTHydrospanner-Mods (or your choice)
Public or Private: Your choice
Initialize: No (let the app create the structure)
```

### 2. Generate GitHub Token
1. Visit: https://github.com/settings/tokens
2. Click "Generate new token (classic)"
3. Name: `XvT Hydrospanner Upload`
4. Permissions: Check `repo` (full control)
5. Click "Generate token"
6. **Copy the token** (you won't see it again!)

### 3. Configure App
1. Open XvT Hydrospanner
2. Click Settings (⚙ button)
3. Expand "Remote Repository Settings"
4. Enter:
   - **Repository Owner**: Your GitHub username
   - **Repository Name**: Your repository name
   - **Branch**: `main` (or your choice)
   - **GitHub Personal Access Token**: Paste your token
5. Click Save

## Uploading Mods

### Workflow
```
Local Import → Test → Upload → Share
```

### Step-by-Step

#### 1. **Import Locally**
- Go to "Mod Warehouse" page
- Click "+ Add File"
- Select your mod file or archive
- Fill in metadata (name, description, version, etc.)
- Click OK

#### 2. **Test the Mod**
- Go to "Profile Management"
- Create or select a profile
- Add the mod to the profile
- Click "▶ Apply Profile"
- Launch the game and verify the mod works

#### 3. **Upload to Remote**
- Go back to "Mod Warehouse"
- **Select the mod file** in the grid
- Click "⬆ Upload to Remote"
- Confirm the upload
- Wait for success message

#### 4. **Share with Others**
The mod is now available! Other users can:
- Click "🌐 Remote Mods" in the app
- See your uploaded mod in the catalog
- Click "Download" to get it

## What Happens During Upload

1. **File Upload**: Mod file is uploaded to `files/{id}.ext` in your repository
2. **Catalog Update**: 
   - If `catalog.json` exists: Updated with new entry
   - If doesn't exist: Created from your local catalog
3. **Metadata Preserved**: All information is maintained:
   - Name, description, version
   - Author, tags, category
   - Target path, file size
   - Download URL (auto-generated)

## First Upload Behavior

**If your repository has no `catalog.json` yet:**
- The app creates one automatically
- Uses your local warehouse catalog as the base
- Adds the new mod entry
- Uploads to GitHub

This makes it super easy to bootstrap a new remote warehouse!

## Troubleshooting

### "GitHub token required"
- You need to set up your token in Settings
- See Setup step 2 above

### "Failed to upload: 403 Forbidden"
- Your token doesn't have the right permissions
- Regenerate with `repo` scope enabled

### "Failed to upload: 404 Not Found"
- Repository doesn't exist
- Check owner/repo name in Settings
- Create the repository on GitHub first

### "Failed to update catalog"
- Check that branch exists (default: `main`)
- Verify token permissions
- Try creating the branch manually on GitHub

## Security Notes

- ⚠️ Token is stored in `%APPDATA%\XvTHydrospanner\config.json`
- Keep this file secure (it's like a password)
- Never share your token publicly
- Revoke token if compromised: github.com/settings/tokens

## Tips

- **Test First**: Always test mods locally before uploading
- **Descriptive Names**: Use clear names so users know what the mod does
- **Version Numbers**: Use semantic versioning (1.0, 1.1, 2.0)
- **Tags**: Add relevant tags for searchability
- **Author Credit**: Fill in author field to give proper credit

## Example: Complete Flow

```
1. Download a cool XvT mission from a forum
   ↓
2. Import to Warehouse: "Enhanced Rebel Campaign v1.5"
   ↓
3. Test in profile: Works great!
   ↓
4. Upload to remote: Click, click, done!
   ↓
5. Friend downloads it through Remote Mods
   ↓
6. Community grows! 🎉
```

## Advanced: Multiple Contributors

You can give other users access to upload:
1. Add them as collaborators on your GitHub repo
2. They configure their app with their own token
3. Everyone can upload to the same repository
4. Build a community mod library!

## Questions?

Check the full documentation:
- `REMOTE_WAREHOUSE_SETUP.md` - Repository structure
- `REMOTE_WAREHOUSE_IMPLEMENTATION.md` - Technical details
