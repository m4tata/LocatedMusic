# LocatedMusic

A Dalamud plugin for FFXIV that plays custom music based on your in-game location and time of day.

## Features

- **Location-based music**: Play different songs in different areas (West Thanalan, East Shroud, etc.)
- **Time of day support**: Configure songs to play during Morning, Midday, or Night
- **MP3 file support**: Use your own MP3 files from a folder
- **Flexible configuration**: Assign multiple areas and times to each song
- **Easy-to-use UI**: Configure everything through an in-game window

## Prerequisites

Before you begin, make sure you have:

1. **XIVLauncher** installed and configured
2. **Dalamud** installed (comes with XIVLauncher)
3. **FFXIV** installed and run at least once with Dalamud enabled
4. **.NET 9.0 SDK** installed (Visual Studio 2022 or Rider will handle this automatically)
5. **Visual Studio 2022** (Community edition is free) or **JetBrains Rider**

## Installation & Setup

### Step 1: Build the Plugin

1. Open `SamplePlugin.sln` in Visual Studio 2022 or Rider
2. In the top toolbar, make sure the configuration is set to **Debug** (or **Release** for a final build)
3. Click **Build** → **Build Solution** (or press `Ctrl+Shift+B`)
4. Wait for the build to complete. You should see "Build succeeded" in the Output window

### Step 2: Locate the Built Plugin

After building, the plugin DLL will be located at:
- **Debug build**: `SamplePlugin\bin\x64\Debug\LocatedMusic.dll`
- **Release build**: `SamplePlugin\bin\x64\Release\LocatedMusic.dll`

**Important**: You need the entire `LocatedMusic` folder, not just the DLL! The folder should contain:
- `LocatedMusic.dll`
- `LocatedMusic.json`
- Any dependencies (NAudio.dll, etc.)

### Step 3: Add Plugin to Dalamud

1. Launch FFXIV through XIVLauncher
2. Once in-game, type `/xlsettings` in the chat to open Dalamud settings
3. Go to the **Experimental** tab
4. In the **Dev Plugin Locations** section, click **Add**
5. Navigate to and select the **full path** to the `LocatedMusic` folder (the one containing the DLL)
   - Example: `D:\Mateos Folder\FFXIV\Plugins\SamplePlugin\SamplePlugin\bin\x64\Debug\LocatedMusic`
6. Click **Save and Close**

### Step 4: Enable the Plugin

1. In-game, type `/xlplugins` to open the Plugin Installer
2. Go to **Dev Tools** → **Installed Dev Plugins**
3. Find **LocatedMusic** in the list
4. Check the box to **Enable** it
5. The plugin is now active!

## Usage

### Opening the Configuration Window

Type `/locatedmusic` in chat to open the configuration window.

### Setting Up Your Music

1. **Choose a Music Folder**:
   - In the configuration window, paste the full path to a folder containing your MP3 files
   - Example: `C:\Users\YourName\Music\FFXIV Music`
   - Click **Scan Folder** to automatically add all MP3 files from that folder

2. **Configure Songs**:
   - Click on a song in the list to select it
   - In the **Song Editor** section below:
     - **Song Name**: Give it a friendly name (optional)
     - **Area Assignments**: 
       - Uncheck "Play in Any Area" to assign specific areas
       - Click "+ Add Area" to add an area
       - Use the dropdown to select an area (e.g., "West Thanalan", "East Shroud")
       - You can add multiple areas per song
       - Click "Remove" next to an area to remove it
     - **Time of Day Assignments**:
       - Uncheck "Play at Any Time" to assign specific times
       - Click "+ Add Time of Day" to add a time
       - Select Morning, Midday, or Night from the dropdown
       - You can add multiple times per song

3. **Master Controls**:
   - **Enable LocatedMusic**: Toggle the plugin on/off
   - **Volume**: Adjust the master volume (0.0 to 1.0)

### How It Works

- The plugin continuously monitors your location and the in-game time
- When you enter a new area or the time of day changes, it finds all songs that match:
  - Your current area (or songs set to "Play in Any Area")
  - The current time of day (or songs set to "Play at Any Time")
- It randomly selects one matching song and plays it
- The song will loop until you change location or time

## Troubleshooting

### Plugin Doesn't Appear in Dev Plugins

- Make sure you added the **folder path**, not just the DLL file
- Check that the folder contains both `LocatedMusic.dll` and `LocatedMusic.json`
- Try restarting the game

### No Music Plays

- Check that "Enable LocatedMusic" is checked in the config window
- Verify your music folder path is correct and contains MP3 files
- Make sure at least one song has matching area and time assignments
- Check the volume slider isn't at 0
- Look at `/xllog` for any error messages

### Songs Don't Change When Moving

- Make sure you've assigned areas to your songs
- Check that the areas are correctly selected in the dropdown
- The plugin checks for location changes every frame, so it should update immediately

### Build Errors

- Make sure you have .NET 9.0 SDK installed
- Try cleaning the solution (Build → Clean Solution) and rebuilding
- Make sure all NuGet packages are restored (right-click solution → Restore NuGet Packages)

## Technical Details

- **Audio Library**: Uses NAudio for MP3 playback
- **Location Detection**: Uses Dalamud's ClientState to get current territory
- **Time Detection**: Calculates Eorzean time based on game's time system
- **Configuration**: Saved in Dalamud's plugin config directory

## Future Enhancements (Not Yet Implemented)

- Spotify integration (requires Spotify API setup)
- Fade in/out between songs
- Playlist shuffling options
- More granular time controls

## Notes

- This plugin plays music **over** the game's music, it doesn't replace it. You may want to turn down the game's BGM volume in FFXIV settings
- The plugin only supports MP3 files currently
- Spotify integration is planned but not yet implemented

## Support

If you encounter issues:
1. Check `/xllog` for error messages
2. Make sure all prerequisites are installed
3. Verify your music files are valid MP3 files
4. Check that your folder paths are correct (use full absolute paths)

---

**Enjoy your custom music experience in FFXIV!**
