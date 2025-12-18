# Modded Installs Feature Implementation

## Overview
This document describes the implementation of the new "Modded Installs" feature that allows users to upload and download complete modded game installations via GitHub branches.

## Changes Made

### 1. Configuration Updates

#### AppConfig.cs
- Added new property: `ModdedInstallsRepositoryName` to store the name of the GitHub repository for modded installations

### 2. Settings Window Updates

#### SettingsWindow.xaml
- Renamed "Repository Name" field to "Mod Library Name" (for clarity)
- Added new field: "Modded Installs Name" for the modded installations repository

#### SettingsWindow.xaml.cs
- Updated `LoadSettings()` to load the new `ModdedInstallsRepositoryName` property
- Updated `SaveButton_Click()` to save the new property

### 3. New Service: ModdedInstallsManager

#### Services/ModdedInstallsManager.cs
A new service that manages modded game installations using GitHub branches:

**Key Features:**
- `GetAvailableBranchesAsync()` - Fetches all branches from the modded installs repository (each branch represents a different modded installation)
- `DownloadBranchAsync()` - Downloads a complete installation from a specific branch
- `UploadInstallationAsync()` - Uploads a local game installation folder as a new branch

**How it works:**
- Each branch in the repository represents a different modded installation
- The "main" branch is displayed as "Base Game Installation"
- Users can download any branch to a local folder
- Users can upload their own modded installations as new branches
- Requires GitHub Personal Access Token for uploading

### 4. New Page: ModdedInstallsPage

#### Views/ModdedInstallsPage.xaml
A new page that displays available modded installations in a tile-based layout:
- Shows all available branches as tiles
- "main" branch is displayed as "Base Game Installation"
- Each tile has a download button
- Header has an upload button for adding new installations

#### Views/ModdedInstallsPage.xaml.cs
Code-behind for the page:
- `ModdedInstall` class - ViewModel for displaying installation info
- Loads available branches on page load
- Handles download button clicks (prompts for destination folder)
- Handles upload button clicks (prompts for folder and branch name)
- Shows progress messages during operations

### 5. New Dialog: InputDialog

#### Views/InputDialog.xaml & InputDialog.xaml.cs
A simple input dialog for getting text input from users:
- Used to prompt for branch names when uploading installations
- Generic dialog that can be reused for other purposes

### 6. Main Window Updates

#### MainWindow.xaml
- Added new navigation button: "📦 Modded Installs" in the left sidebar

#### MainWindow.xaml.cs
- Added `_moddedInstallsManager` field
- Initialized `ModdedInstallsManager` with configuration values
- Added `ModdedInstallsButton_Click()` handler to navigate to the new page

## Usage

### For End Users

**Downloading a Modded Installation:**
1. Click "📦 Modded Installs" in the navigation menu
2. Browse available installations displayed as tiles
3. Click "⬇ Download" on desired installation
4. Select a destination folder
5. Wait for download and extraction to complete

**Uploading a Modded Installation:**
1. Configure GitHub Personal Access Token in Settings (required for uploads)
2. Click "📦 Modded Installs" in the navigation menu
3. Click "⬆ Upload Install" button
4. Select the game installation folder you want to upload
5. Enter a branch name (no spaces or special characters)
6. Confirm the upload
7. Wait for upload to complete

### Repository Setup

**Mod Library Repository** (existing):
- Stores individual mod packages
- Example: `XvTHydrospanner-Mods`

**Modded Installs Repository** (new):
- Stores complete game installations
- Each branch is a different modded installation
- "main" branch should contain the base/clean game installation
- Example: `XvTHydrospanner-Installs`

## Technical Details

### GitHub Integration
- Uses GitHub REST API v3
- Downloads use public archive endpoints (no auth required)
- Uploads use Git Data API (requires Personal Access Token with repo permissions)
- Upload process:
  1. Create new branch from main
  2. Create blob for each file
  3. Create tree with all blobs
  4. Create commit with tree
  5. Update branch reference

### Default Values
- Repository Owner: `RahlForge` (if not configured)
- Mod Library Name: `XvTHydrospanner-Mods`
- Modded Installs Name: `XvTHydrospanner-Installs`
- Branch: `main`

## Notes

- Large installations may take considerable time to upload (all files are uploaded individually)
- GitHub has file size limits (100MB per file, 100GB per repository)
- Download extracts files and removes the GitHub-added root folder automatically
- Branch names must not contain spaces or special characters
- The "main" branch is special-cased to display as "Base Game Installation"

## Future Enhancements

Potential improvements for future versions:
- Progress bar with percentage for uploads/downloads
- Compression optimization for faster uploads
- Resume capability for interrupted transfers
- Installation metadata (description, author, version, screenshots)
- Diff view between installations
- Direct installation to game folder option

