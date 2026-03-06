# XvT Hydrospanner - Architecture Diagram

## System Architecture Overview

```
┌─────────────────────────────────────────────────────────────────────────┐
│                          XvT HYDROSPANNER                                │
│                    Mod Manager Application                               │
└─────────────────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────────────────┐
│                           PRESENTATION LAYER                             │
│                              (WPF Views)                                 │
├─────────────────────────────────────────────────────────────────────────┤
│                                                                          │
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐  ┌──────────┐  │
│  │  MainWindow  │  │   Profile    │  │  Warehouse   │  │  Active  │  │
│  │              │  │  Management  │  │    Page      │  │   Mods   │  │
│  │  - Header    │  │    Page      │  │              │  │   Page   │  │
│  │  - Nav Menu  │  │  - List      │  │  - DataGrid  │  │          │  │
│  │  - Content   │  │  - Details   │  │  - Search    │  │ - List   │  │
│  │  - Status    │  │  - Mods      │  │  - Add/Del   │  │          │  │
│  └──────────────┘  └──────────────┘  └──────────────┘  └──────────┘  │
│                                                                          │
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐  ┌──────────┐  │
│  │   Settings   │  │ New Profile  │  │ Add Warehouse│  │   Add    │  │
│  │   Window     │  │   Dialog     │  │File Dialog   │  │   Mod    │  │
│  │              │  │              │  │              │  │  Dialog  │  │
│  │  - Paths     │  │  - Name      │  │  - Metadata  │  │          │  │
│  │  - Options   │  │  - Desc      │  │  - Category  │  │ - Select │  │
│  └──────────────┘  └──────────────┘  └──────────────┘  └──────────┘  │
│                                                                          │
└─────────────────────────────────────────────────────────────────────────┘
                                    │
                                    ▼
┌─────────────────────────────────────────────────────────────────────────┐
│                           BUSINESS LOGIC LAYER                           │
│                              (Services)                                  │
├─────────────────────────────────────────────────────────────────────────┤
│                                                                          │
│  ┌──────────────────┐  ┌──────────────────┐  ┌────────────────────┐   │
│  │ ProfileManager   │  │ WarehouseManager │  │  ModApplicator     │   │
│  │                  │  │                  │  │                    │   │
│  │ - Create         │  │ - Add File       │  │ - Apply Mod        │   │
│  │ - Load/Save      │  │ - Remove File    │  │ - Revert Mod       │   │
│  │ - Delete         │  │ - Search         │  │ - Backup File      │   │
│  │ - Clone          │  │ - Get by Category│  │ - Verify File      │   │
│  │ - Set Active     │  │ - Load Catalog   │  │ - Cleanup Backups  │   │
│  │ - Events         │  │ - Events         │  │ - Events           │   │
│  └──────────────────┘  └──────────────────┘  └────────────────────┘   │
│                                                                          │
│  ┌──────────────────────────────────────────────────────────────────┐  │
│  │              ConfigurationManager                                 │  │
│  │                                                                   │  │
│  │  - Load Config    - Save Config    - Validate Config            │  │
│  │  - Set Paths      - Get Config     - Events                     │  │
│  └──────────────────────────────────────────────────────────────────┘  │
│                                                                          │
└─────────────────────────────────────────────────────────────────────────┘
                                    │
                                    ▼
┌─────────────────────────────────────────────────────────────────────────┐
│                              DATA LAYER                                  │
│                            (Models & Data)                               │
├─────────────────────────────────────────────────────────────────────────┤
│                                                                          │
│  ┌─────────────┐  ┌──────────────┐  ┌──────────────┐  ┌──────────┐   │
│  │ ModProfile  │  │FileModifica- │  │WarehouseFile │  │AppConfig │   │
│  │             │  │    tion      │  │              │  │          │   │
│  │ - Id        │  │ - Id         │  │ - Id         │  │ - Paths  │   │
│  │ - Name      │  │ - GamePath   │  │ - Name       │  │ - Settings│  │
│  │ - Desc      │  │ - WarehouseId│  │ - TargetPath │  │ - Active │   │
│  │ - Mods[]    │  │ - BackupPath │  │ - Category   │  │   Profile│   │
│  │ - IsActive  │  │ - Category   │  │ - Storage    │  │          │   │
│  │ - Dates     │  │ - IsApplied  │  │ - Metadata   │  │          │   │
│  └─────────────┘  └──────────────┘  └──────────────┘  └──────────┘   │
│                                                                          │
│  ┌──────────────────────────────────────────────────────────────────┐  │
│  │                   GameFileCategory                                │  │
│  │                                                                   │  │
│  │  Predefined categories: Battle, Combat, Graphics, Sound, etc.   │  │
│  └──────────────────────────────────────────────────────────────────┘  │
│                                                                          │
└─────────────────────────────────────────────────────────────────────────┘
                                    │
                                    ▼
┌─────────────────────────────────────────────────────────────────────────┐
│                          STORAGE LAYER                                   │
│                        (File System & JSON)                              │
├─────────────────────────────────────────────────────────────────────────┤
│                                                                          │
│  ┌────────────────────────────────────────────────────────────────┐    │
│  │              %APPDATA%\XvTHydrospanner\                        │    │
│  │                                                                 │    │
│  │  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐        │    │
│  │  │   Profiles/  │  │  Warehouse/  │  │   Backups/   │        │    │
│  │  │              │  │              │  │              │        │    │
│  │  │ {guid}.json │  │ catalog.json │  │ {mod-id}_    │        │    │
│  │  │ {guid}.json │  │ {guid}.TIE   │  │ {timestamp}_ │        │    │
│  │  │    ...      │  │ {guid}.LST   │  │ {filename}   │        │    │
│  │  └──────────────┘  │    ...      │  │    ...       │        │    │
│  │                    └──────────────┘  └──────────────┘        │    │
│  │                                                                 │    │
│  │  ┌──────────────┐                                             │    │
│  │  │ config.json  │  ← Application configuration                │    │
│  │  └──────────────┘                                             │    │
│  └────────────────────────────────────────────────────────────────┘    │
│                                                                          │
└─────────────────────────────────────────────────────────────────────────┘
                                    │
                                    ▼
┌─────────────────────────────────────────────────────────────────────────┐
│                           TARGET SYSTEM                                  │
│                    Star Wars: X-Wing vs TIE Fighter                      │
├─────────────────────────────────────────────────────────────────────────┤
│                                                                          │
│  Game Install Directory (e.g., C:\GOG Games\Star Wars - XvT\)          │
│                                                                          │
│  ┌────────────────────────────────────────────────────────────────┐    │
│  │  BalanceOfPower/                                               │    │
│  │  ├── BATTLE/    ← Modified .TIE, .LST files                   │    │
│  │  ├── CAMPAIGN/  ← Modified campaign missions                  │    │
│  │  ├── TRAIN/     ← Modified training missions                  │    │
│  │  └── ...                                                        │    │
│  │                                                                 │    │
│  │  Combat/        ← Combat missions                              │    │
│  │  cp320/         ← Low-res graphics (modified)                  │    │
│  │  cp640/         ← High-res graphics (modified)                 │    │
│  │  Music/         ← Music files (modified)                       │    │
│  │  wave/          ← Sound effects (modified)                     │    │
│  │  resource/      ← Game resources (modified)                    │    │
│  └────────────────────────────────────────────────────────────────┘    │
│                                                                          │
└─────────────────────────────────────────────────────────────────────────┘
```

## Data Flow Diagrams

### Profile Application Flow

```
┌──────────┐
│  User    │
│ Selects  │
│ Profile  │
└────┬─────┘
     │
     ▼
┌─────────────────┐
│  MainWindow     │
│ Gets Active     │
│ Profile         │
└────┬────────────┘
     │
     ▼
┌──────────────────┐
│ ProfileManager   │
│ Returns Profile  │
│ with Mods[]      │
└────┬─────────────┘
     │
     ▼
┌──────────────────┐      ┌──────────────────┐
│ User Clicks      │      │ ModApplicator    │
│ "Apply Profile"  │─────▶│ ApplyProfile()   │
└──────────────────┘      └────┬─────────────┘
                               │
                   ┌───────────┴──────────┐
                   │                      │
                   ▼                      ▼
        ┌─────────────────┐    ┌─────────────────┐
        │ For Each Mod:   │    │ WarehouseManager│
        │ 1. Get warehouse│◀───│ GetFile()       │
        │    file         │    └─────────────────┘
        │ 2. Backup orig  │
        │ 3. Copy file    │
        │ 4. Mark applied │
        └────┬────────────┘
             │
             ▼
        ┌─────────────────┐
        │ File System     │
        │ Original → Bkp  │
        │ Warehouse → Game│
        └─────────────────┘
```

### Warehouse File Addition Flow

```
┌──────────┐
│  User    │
│ Browses  │
│ for File │
└────┬─────┘
     │
     ▼
┌─────────────────────┐
│ AddWarehouseFile    │
│ Dialog              │
│ - Displays metadata │
│   form              │
└────┬────────────────┘
     │
     ▼
┌─────────────────────┐
│ User Fills:         │
│ - Name              │
│ - Target Path       │
│ - Category          │
│ - Description       │
└────┬────────────────┘
     │
     ▼
┌──────────────────────┐
│ WarehouseManager     │
│ AddFileAsync()       │
│                      │
│ 1. Generate GUID     │
│ 2. Copy to warehouse │
│ 3. Create metadata   │
│ 4. Update catalog    │
└────┬─────────────────┘
     │
     ▼
┌──────────────────────┐
│ File System          │
│ Source → Warehouse/  │
│ {guid}.ext           │
│                      │
│ Update catalog.json  │
└──────────────────────┘
```

### Profile Switching Flow

```
┌──────────┐
│  User    │
│ Switches │
│ Profile  │
└────┬─────┘
     │
     ▼
┌─────────────────────┐      ┌──────────────────┐
│ Check if Current    │ Yes  │ Prompt User:     │
│ Profile Has Applied │─────▶│ Revert Current?  │
│ Modifications       │      └────┬─────────────┘
└────┬────────────────┘           │
     │ No                          ▼
     │                    ┌──────────────────┐
     │                    │ ModApplicator    │
     │                    │ RevertProfile()  │
     │                    └────┬─────────────┘
     │                         │
     └─────────────────────────┘
                 │
                 ▼
        ┌─────────────────────┐
        │ ProfileManager      │
        │ SetActiveProfile()  │
        │                     │
        │ 1. Deactivate old   │
        │ 2. Activate new     │
        │ 3. Save both        │
        │ 4. Fire events      │
        └────┬────────────────┘
             │
             ▼
        ┌─────────────────────┐
        │ ConfigurationManager│
        │ SetActiveProfile()  │
        │ Save config.json    │
        └────┬────────────────┘
             │
             ▼
        ┌─────────────────────┐
        │ UI Updates          │
        │ - Refresh profile   │
        │ - Update status     │
        └─────────────────────┘
```

## Component Interaction Matrix

```
┌──────────────┬──────────┬──────────┬──────────┬──────────┐
│              │ Profile  │Warehouse │   Mod    │  Config  │
│              │ Manager  │ Manager  │Applicator│ Manager  │
├──────────────┼──────────┼──────────┼──────────┼──────────┤
│ MainWindow   │    ✓     │    ✓     │    ✓     │    ✓     │
├──────────────┼──────────┼──────────┼──────────┼──────────┤
│ ProfileMgmt  │    ✓     │    ✓     │          │          │
│ Page         │          │          │          │          │
├──────────────┼──────────┼──────────┼──────────┼──────────┤
│ Warehouse    │          │    ✓     │          │          │
│ Page         │          │          │          │          │
├──────────────┼──────────┼──────────┼──────────┼──────────┤
│ ActiveMods   │    ✓     │    ✓     │          │          │
│ Page         │          │          │          │          │
├──────────────┼──────────┼──────────┼──────────┼──────────┤
│ Settings     │          │          │          │    ✓     │
│ Window       │          │          │          │          │
├──────────────┼──────────┼──────────┼──────────┼──────────┤
│ ModApplicator│          │    ✓     │    -     │          │
└──────────────┴──────────┴──────────┴──────────┴──────────┘

✓ = Uses this service
- = Self reference
```

## Technology Stack Diagram

```
┌─────────────────────────────────────────────────────────┐
│                  Application Layer                      │
│                  XvT Hydrospanner                       │
│                                                         │
│  ┌───────────────────────────────────────────────────┐ │
│  │              WPF Framework                        │ │
│  │  ┌──────────┐  ┌──────────┐  ┌──────────┐       │ │
│  │  │  XAML    │  │   Data   │  │ Commands │       │ │
│  │  │ Markup   │  │ Binding  │  │  Events  │       │ │
│  │  └──────────┘  └──────────┘  └──────────┘       │ │
│  └───────────────────────────────────────────────────┘ │
│                                                         │
│  ┌───────────────────────────────────────────────────┐ │
│  │              .NET 8.0 Runtime                     │ │
│  │  ┌──────────┐  ┌──────────┐  ┌──────────┐       │ │
│  │  │   C#     │  │  Async/  │  │  LINQ    │       │ │
│  │  │ Language │  │  Await   │  │          │       │ │
│  │  └──────────┘  └──────────┘  └──────────┘       │ │
│  └───────────────────────────────────────────────────┘ │
│                                                         │
│  ┌───────────────────────────────────────────────────┐ │
│  │           NuGet Packages                          │ │
│  │  ┌────────────────┐  ┌─────────────────────┐    │ │
│  │  │ Newtonsoft.Json│  │ CommunityToolkit.   │    │ │
│  │  │    (13.0.3)    │  │    Mvvm (8.2.2)     │    │ │
│  │  └────────────────┘  └─────────────────────┘    │ │
│  └───────────────────────────────────────────────────┘ │
└─────────────────────────────────────────────────────────┘
                        │
                        ▼
┌─────────────────────────────────────────────────────────┐
│              Operating System Layer                     │
│                  Windows 10/11                          │
│                                                         │
│  ┌───────────────────────────────────────────────────┐ │
│  │          File System APIs                         │ │
│  │  - Directory operations                           │ │
│  │  - File I/O (async)                               │ │
│  │  - Path manipulation                              │ │
│  └───────────────────────────────────────────────────┘ │
│                                                         │
│  ┌───────────────────────────────────────────────────┐ │
│  │          User Interface                           │ │
│  │  - Win32 windowing                                │ │
│  │  - DirectX rendering                              │ │
│  │  - Input handling                                 │ │
│  └───────────────────────────────────────────────────┘ │
└─────────────────────────────────────────────────────────┘
```

## Deployment Diagram

```
┌─────────────────────────────────────────────────────────┐
│           Developer Machine                             │
│                                                         │
│  ┌────────────────────────────────────────────────┐   │
│  │  Visual Studio 2022                            │   │
│  │  ┌──────────────────────────────────────────┐ │   │
│  │  │  XvTHydrospanner.sln                     │ │   │
│  │  │  - Build (Debug/Release)                 │ │   │
│  │  │  - Restore NuGet packages                │ │   │
│  │  │  - Compile C# and XAML                   │ │   │
│  │  └──────────────────────────────────────────┘ │   │
│  └────────────────────────────────────────────────┘   │
│                          │                             │
│                          ▼                             │
│  ┌────────────────────────────────────────────────┐   │
│  │  Build Output                                  │   │
│  │  bin/Release/net8.0-windows/                   │   │
│  │  - XvTHydrospanner.exe                         │   │
│  │  - *.dll dependencies                          │   │
│  │  - config files                                │   │
│  └────────────────────────────────────────────────┘   │
└─────────────────────────────────────────────────────────┘
                          │
                          │ Deploy
                          ▼
┌─────────────────────────────────────────────────────────┐
│           End User Machine                              │
│                                                         │
│  ┌────────────────────────────────────────────────┐   │
│  │  Program Files/                                │   │
│  │  XvTHydrospanner/                              │   │
│  │  - XvTHydrospanner.exe                         │   │
│  │  - Dependencies                                │   │
│  │  - README.md                                   │   │
│  └────────────────────────────────────────────────┘   │
│                          │                             │
│                          │ Reads/Writes               │
│                          ▼                             │
│  ┌────────────────────────────────────────────────┐   │
│  │  %APPDATA%/XvTHydrospanner/                    │   │
│  │  - config.json                                 │   │
│  │  - Profiles/                                   │   │
│  │  - Warehouse/                                  │   │
│  │  - Backups/                                    │   │
│  └────────────────────────────────────────────────┘   │
│                          │                             │
│                          │ Modifies                   │
│                          ▼                             │
│  ┌────────────────────────────────────────────────┐   │
│  │  C:\GOG Games\Star Wars - XvT\                 │   │
│  │  - Modified game files                         │   │
│  │  - Original files backed up                    │   │
│  └────────────────────────────────────────────────┘   │
└─────────────────────────────────────────────────────────┘
```

---

**Diagrams created**: December 3, 2025
**Format**: ASCII/Text-based for universal compatibility
