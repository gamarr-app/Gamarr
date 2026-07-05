# Gamarr Playnite Library Plugin

A [Playnite](https://playnite.link) library plugin that imports your
[Gamarr](https://github.com/gamarr/gamarr) game library into Playnite.
Every game that Gamarr has downloaded shows up in Playnite with its title,
release date, install directory, platforms, genres, developer/publisher,
description, poster cover, and links to Steam/IGDB/RAWG and back to Gamarr.

## What gets imported

- By default only games Gamarr has an imported file for
  (`hasFile == true` or `sizeOnDisk > 0`). A settings toggle also imports
  monitored-but-not-downloaded games as "not installed".
- Downloaded games are marked *installed* in Playnite with
  `InstallDirectory` set to Gamarr's game path, so Playnite's
  "is installed" filter works. No play action is created — Gamarr manages
  files, not launchers; point Playnite at the executable yourself or let
  other extensions handle launching.
- Covers are served straight from your Gamarr server
  (`/api/v3/mediacover/{id}/poster.jpg`), so the server must be reachable
  from the Playnite machine during import.

## Requirements

- Windows with Playnite 10/11 (SDK 6.x)
- A reachable Gamarr server and its API key
  (Gamarr UI: Settings → General → Security, or `<ApiKey>` in `config.xml`)

## Build

The plugin targets `net462` (Playnite's plugin runtime) but builds on any
OS with the .NET SDK, via reference assemblies:

```bash
cd contrib/playnite
./build-pext.sh          # produces dist/GamarrLibrary_<version>.pext
```

Or manually: `dotnet build GamarrLibrary/GamarrLibrary.csproj -c Release`
and zip `GamarrLibrary.dll`, `Newtonsoft.Json.dll` and `extension.yaml`
into a file with a `.pext` extension (do **not** include
`Playnite.SDK.dll`).

This project is intentionally **not** part of `src/Gamarr.sln`.

## Install

1. Build or download `GamarrLibrary_<version>.pext`.
2. Drag & drop the `.pext` onto the Playnite window (or double-click it)
   and confirm the install prompt; Playnite restarts.
3. In Playnite: *Add-ons… (F9) → Extensions settings → Libraries → Gamarr
   Library*, enter your Gamarr URL (e.g. `http://localhost:6767`) and API
   key, save.
4. Update the library: *Menu → Update Game Library → Gamarr*.

## Settings

| Setting | Meaning |
| --- | --- |
| Gamarr URL | Base URL of the server, e.g. `http://192.168.1.10:6767` |
| API key | Gamarr API key, sent as `X-Api-Key` |
| Also import not-downloaded games | Import monitored games without files as "not installed" |

The settings view is a minimal code-built WPF panel (two text boxes and a
checkbox) rather than a styled XAML view — kept XAML-free so the project
compiles with plain reference assemblies on macOS/Linux.

## Tests

The Gamarr→Playnite mapping logic lives in `GamarrLibrary/Mapping/` with
no PlayniteSDK dependency and is covered by NUnit tests that run on
modern .NET on any OS:

```bash
cd contrib/playnite/GamarrLibrary.Tests
dotnet test
```

## Layout

```
contrib/playnite/
├── build-pext.sh                    # builds + zips the .pext
├── GamarrLibrary/
│   ├── GamarrLibrary.csproj         # net462, PlayniteSDK + Newtonsoft.Json
│   ├── extension.yaml               # Playnite extension manifest
│   ├── GamarrLibraryPlugin.cs       # LibraryPlugin subclass (SDK adapter)
│   ├── GamarrClient.cs              # thin HTTP client for /api/v3
│   ├── GamarrLibrarySettings.cs     # settings + ISettings view model
│   ├── GamarrLibrarySettingsView.cs # code-only WPF settings view
│   └── Mapping/                     # PlayniteSDK-free mapping logic
│       ├── GamarrGameDto.cs
│       ├── MappedGame.cs
│       └── GamarrMapper.cs
└── GamarrLibrary.Tests/             # net10.0 NUnit tests for Mapping/
```
