# LocatedMusic - Troubleshooting Guide

## How It Works

### Music Playback
- **The plugin plays music OVER the game's music** - it does NOT replace it
- It adds a new audio layer on top of the game's BGM
- **You need to manually turn down or mute the game's BGM** in FFXIV's sound settings if you want to hear only your custom music

### When Music Plays
- The plugin checks your location and time of day every frame
- When you enter a new area OR the time of day changes, it:
  1. Finds all songs that match your current area AND time
  2. Randomly picks one
  3. Plays it (replaces current song if different)

### Initial Playback
- The plugin now checks for music when it first loads (after you log in)
- If you're already in an area when the plugin loads, it will check and play music
- Use the "Force Check" button in the config window to manually trigger a check

## Why Music Might Not Play

### 1. Check Song Assignments
- **Both area AND time must match** (unless you check "Play in Any Area" or "Play at Any Time")
- If a song has:
  - "Play in Any Area" = unchecked AND no areas assigned → **Won't play anywhere**
  - "Play at Any Time" = unchecked AND no times assigned → **Won't play at any time**
- Make sure at least one song has proper assignments

### 2. Check File Paths
- Verify the MP3 files actually exist at the paths shown
- Check that file paths are correct (no typos, correct drive letters)
- Make sure files are valid MP3 files (not corrupted)

### 3. Check Plugin Status
- Make sure "Enable LocatedMusic" is checked
- Check volume isn't at 0
- Verify you're logged into the game (plugin won't work on character select)

### 4. Check Logs
- Type `/xllog` in chat to open the Dalamud log window
- Look for messages from "LocatedMusic"
- Common messages:
  - `"No matching songs found for territory X at Y"` → No songs match your current location/time
  - `"Song file not found: [path]"` → File path is wrong or file was moved
  - `"Failed to play song: [path]"` → File might be corrupted or unsupported format

### 5. Test with "Force Check"
- Click the "Force Check" button in the config window
- This manually triggers a music check
- Check the log (`/xllog`) to see what happens

### 6. Test with "Play at Any Time" and "Play in Any Area"
- For testing, set a song to:
  - ✅ "Play in Any Area" = checked
  - ✅ "Play at Any Time" = checked
- This should make it play everywhere, anytime
- If it still doesn't play, the issue is likely with file paths or the audio system

## Common Issues

### "Playing: No" but song should match
1. Check `/xllog` for error messages
2. Verify the territory ID matches (shown in config window)
3. Make sure song has both area AND time assignments (or the "any" checkboxes)
4. Click "Force Check" to manually trigger

### Music plays but you can't hear it
- **Turn down FFXIV's BGM volume** in game settings
- Check the plugin's volume slider
- Make sure your system volume isn't muted
- Check Windows volume mixer

### Music stops when changing areas
- This is normal - it's checking for a new song
- If no matching song is found, it stops
- Make sure you have songs assigned to the new area

### Music doesn't change when time changes
- Time of day changes slowly (every few minutes)
- The plugin checks every frame, so it should update
- Use "Force Check" to manually trigger

## Debugging Steps

1. **Enable the plugin** (checkbox in config)
2. **Set volume** to a reasonable level (0.5-1.0)
3. **Configure a test song**:
   - Check "Play in Any Area"
   - Check "Play at Any Time"
   - This should play everywhere
4. **Click "Force Check"** button
5. **Check `/xllog`** for messages
6. **Verify file exists** at the path shown
7. **Check territory ID** matches what you assigned

## What the Logs Tell You

- `"Checking songs for territory X at Y"` → Plugin is checking
- `"Song matches: [name]"` → Found a matching song
- `"Selected song: [name]"` → Picked this song to play
- `"Attempting to play: [path]"` → Trying to start playback
- `"Now playing: [filename]"` → Successfully started
- `"No matching songs found"` → No songs match current location/time
- `"Song file not found"` → File path issue
- `"Failed to play song"` → Audio playback error (check exception details)

## Still Not Working?

1. Check `/xllog` for detailed error messages
2. Verify MP3 files play in Windows Media Player or similar
3. Try a different MP3 file (in case one is corrupted)
4. Make sure NAudio.dll is in the plugin folder
5. Restart the game and reload the plugin

---

**Remember**: The plugin plays music OVER the game's music. You must turn down BGM in FFXIV settings to hear only your custom music!

