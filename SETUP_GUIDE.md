# LocatedMusic - Complete Setup Guide

## What This Plugin Does

LocatedMusic is a Dalamud plugin that plays your custom MP3 music files based on:
- **Where you are** in the game (area/territory like "West Thanalan", "East Shroud", etc.)
- **What time of day** it is in-game (Morning, Midday, or Night)

You can assign multiple areas and multiple times to each song, giving you complete control over when music plays.

## What I've Built For You

✅ **Complete plugin code** - All files are ready
✅ **Music player** - Plays MP3 files using NAudio library
✅ **Location tracking** - Automatically detects your current area
✅ **Time tracking** - Detects in-game time of day (Morning/Midday/Night)
✅ **Configuration UI** - Full in-game interface for managing songs
✅ **Area selection** - Dropdown with all FFXIV areas
✅ **Time selection** - Easy time of day assignment
✅ **Song management** - Add, remove, and configure songs

## What You Need To Do

### Step 1: Install Prerequisites

1. **Visual Studio 2022** (Community edition is free)
   - Download from: https://visualstudio.microsoft.com/downloads/
   - During installation, make sure to select:
     - ✅ ".NET desktop development" workload
     - ✅ ".NET 9.0 SDK" (should be included automatically)

2. **Verify XIVLauncher and Dalamud are installed**
   - If you're reading this, you probably already have these
   - Make sure you've run FFXIV with Dalamud at least once

### Step 2: Build the Plugin

1. **Open the project**:
   - Navigate to: `D:\Mateos Folder\FFXIV\Plugins\SamplePlugin`
   - Double-click `SamplePlugin.sln` to open it in Visual Studio

2. **Restore NuGet packages** (if needed):
   - Right-click the solution in Solution Explorer
   - Click "Restore NuGet Packages"
   - Wait for it to finish

3. **Build the solution**:
   - In Visual Studio, go to **Build** → **Build Solution** (or press `Ctrl+Shift+B`)
   - Wait for "Build succeeded" message
   - If you see errors, make sure .NET 9.0 SDK is installed

4. **Find your built plugin**:
   - Go to: `SamplePlugin\bin\x64\Debug\LocatedMusic\`
   - You should see:
     - `LocatedMusic.dll`
     - `LocatedMusic.json`
     - Other dependency files (NAudio.dll, etc.)

### Step 3: Add Plugin to Dalamud

1. **Launch FFXIV** through XIVLauncher

2. **Open Dalamud settings**:
   - Type `/xlsettings` in chat
   - Or open Dalamud console and type `xlsettings`

3. **Add plugin location**:
   - Go to **Experimental** tab
   - Find **Dev Plugin Locations**
   - Click **Add** button
   - Navigate to and select the **LocatedMusic folder** (the one with the DLL)
     - Full path example: `D:\Mateos Folder\FFXIV\Plugins\SamplePlugin\SamplePlugin\bin\x64\Debug\LocatedMusic`
   - Click **OK** then **Save and Close**

4. **Enable the plugin**:
   - Type `/xlplugins` in chat
   - Go to **Dev Tools** → **Installed Dev Plugins**
   - Find **LocatedMusic** in the list
   - Check the box to enable it
   - The plugin should now be active!

### Step 4: Configure Your Music

1. **Prepare your music folder**:
   - Create a folder somewhere with your MP3 files
   - Example: `C:\Users\YourName\Music\FFXIV Music`
   - Put your MP3 files in this folder

2. **Open configuration**:
   - Type `/locatedmusic` in chat
   - The configuration window will open

3. **Scan for music**:
   - Paste your music folder path in the "Folder Path" field
   - Click **Scan Folder**
   - All MP3 files will be added to the song list

4. **Configure a song**:
   - Click on a song in the list to select it
   - In the **Song Editor** section:
     - **Song Name**: Give it a name (optional, defaults to filename)
     - **Area Assignments**:
       - Uncheck "Play in Any Area" if you want specific areas
       - Click "+ Add Area"
       - Select an area from the dropdown (e.g., "West Thanalan")
       - Add more areas if you want this song in multiple places
     - **Time of Day Assignments**:
       - Uncheck "Play at Any Time" if you want specific times
       - Click "+ Add Time of Day"
       - Select Morning, Midday, or Night
       - Add more times if you want

5. **Test it**:
   - Make sure "Enable LocatedMusic" is checked
   - Set volume to a comfortable level
   - Travel to an area you configured
   - Music should start playing!

## How It Works

- The plugin runs in the background and checks your location every frame
- When you enter a new area OR the time of day changes:
  - It finds all songs that match your current area AND time
  - It randomly picks one and plays it
  - The song loops until you change location or time

## Tips & Tricks

- **"Play in Any Area"**: Check this if you want a song to play everywhere
- **"Play at Any Time"**: Check this if you want a song regardless of time
- **Multiple assignments**: You can assign the same song to many areas/times
- **Volume control**: Adjust the master volume slider
- **Disable quickly**: Uncheck "Enable LocatedMusic" to stop all music

## Troubleshooting

### "Plugin doesn't appear in Dev Plugins"
- Make sure you added the **folder path**, not just the DLL
- The folder must contain both `LocatedMusic.dll` and `LocatedMusic.json`
- Try restarting the game

### "No music plays"
- Check that "Enable LocatedMusic" is checked
- Verify your music folder path is correct
- Make sure at least one song has area AND time assignments
- Check volume isn't at 0
- Type `/xllog` to see error messages

### "Build errors"
- Make sure .NET 9.0 SDK is installed
- Try: Build → Clean Solution, then Build → Rebuild Solution
- Right-click solution → Restore NuGet Packages

### "Songs don't change when I move"
- Make sure you've assigned areas to your songs
- Check the area names in the dropdown match where you are
- The plugin updates every frame, so it should be instant

## Important Notes

⚠️ **This plugin plays music OVER the game's music** - it doesn't replace it. You may want to:
- Turn down BGM volume in FFXIV's sound settings
- Or mute BGM entirely if you only want your custom music

⚠️ **Only MP3 files are supported** - Other formats (WAV, OGG, etc.) won't work

⚠️ **Spotify integration is NOT implemented yet** - This would require:
  - Spotify API setup
  - OAuth authentication
  - Additional libraries
  - This is a future enhancement

## File Structure

After building, your plugin folder should look like:
```
LocatedMusic/
├── LocatedMusic.dll          (Main plugin)
├── LocatedMusic.json         (Plugin metadata)
├── NAudio.dll                (Audio library)
└── (other dependency files)
```

## Rebuilding After Changes

If you modify the code:
1. Make your changes
2. Build → Build Solution
3. The DLL will be updated automatically
4. Restart the game or reload the plugin (disable then enable in /xlplugins)

## Need Help?

- Check `/xllog` for detailed error messages
- Make sure all prerequisites are installed
- Verify file paths are correct (use full absolute paths)
- Check that MP3 files are valid and not corrupted

---

**You're all set! Enjoy your custom music experience in FFXIV! 🎵**

