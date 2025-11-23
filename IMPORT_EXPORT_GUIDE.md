# Import/Export Guide - LocatedMusic

## Individual Song Settings

### Export Settings
1. Select a song from the list
2. In the "Song Editor" section, click **"Export Settings"**
3. The settings are copied to your clipboard
4. You can paste it anywhere (text file, share with others, etc.)

### Import Settings
1. Select a song from the list
2. Copy the exported settings code to your clipboard
3. Click **"Import Settings"** in the "Song Editor" section
4. The settings are applied to the selected song

### Format
The exported code looks like this:
```
AREAS:West Thanalan,East Thanalan,Central Thanalan|TIMES:Morning,Midday,Night|PLAYINANYAREA:false|PLAYATANYTIME:false
```

**Structure:**
- `AREAS:` - Comma-separated list of area names
- `TIMES:` - Comma-separated list of times (Morning, Midday, Night)
- `PLAYINANYAREA:` - true or false
- `PLAYATANYTIME:` - true or false

### Example Usage
**Export:**
1. Configure a song with areas and times
2. Click "Export Settings"
3. Share the code with a friend

**Import:**
1. Friend sends you: `AREAS:West Thanalan,East Thanalan|TIMES:Morning,Midday|PLAYINANYAREA:false|PLAYATANYTIME:false`
2. Copy it to clipboard
3. Select your song
4. Click "Import Settings"
5. Settings are applied!

## Playlist Sharing (All Songs)

### Export All
1. Click **"Export All"** next to "Song Configuration" title
2. All your songs' settings are copied to clipboard
3. Share with others or save as backup

### Import All
1. Make sure you have the same songs in your song list (same file paths)
2. Copy the playlist code to clipboard
3. Click **"Import All"** next to "Song Configuration" title
4. Settings are applied to all matching songs

### Format
The exported playlist looks like this:
```
LOCATEDMUSIC_PLAYLIST_V1
SONG:C:\Music\song1.mp3|AREAS:West Thanalan|TIMES:Morning|PLAYINANYAREA:false|PLAYATANYTIME:false
SONG:C:\Music\song2.mp3|AREAS:East Thanalan|TIMES:Midday|PLAYINANYAREA:false|PLAYATANYTIME:false
SONG:C:\Music\song3.mp3|AREAS:Central Thanalan|TIMES:Night|PLAYINANYAREA:false|PLAYATANYTIME:false
```

**Structure:**
- First line: `LOCATEDMUSIC_PLAYLIST_V1` (version identifier)
- Each following line: One song's settings
  - `SONG:` - Full file path (used to match songs)
  - `AREAS:` - Comma-separated area names
  - `TIMES:` - Comma-separated times
  - `PLAYINANYAREA:` - true or false
  - `PLAYATANYTIME:` - true or false

### Important Notes

**For Playlist Import:**
- Songs are matched by **file path**
- If a song's file path doesn't match, it will be skipped
- Only songs that exist in your song list will be updated
- Song names don't matter - only file paths are used for matching

**Area Names:**
- Must match exactly (case-insensitive)
- If an area name doesn't match, it will be skipped
- Check `/xllog` for warnings about unmatched areas

**Time Names:**
- Must be: `Morning`, `Midday`, or `Night` (case-insensitive)
- Invalid times will be skipped

## Use Cases

### 1. Share Settings with Friends
- Export your song settings
- Friend imports them to their matching songs
- Both have the same music experience!

### 2. Backup Your Configuration
- Export all songs before making changes
- If something goes wrong, import the backup
- Never lose your carefully configured settings!

### 3. Quick Setup
- Someone shares a playlist code
- Import it to quickly set up multiple songs
- Much faster than configuring each song manually

### 4. Transfer Between Characters/Accounts
- Export your playlist
- Import on another character/account
- Same music setup everywhere!

## Troubleshooting

### "Clipboard is empty"
- Make sure you copied the code before clicking Import
- Try copying again

### "Song not found" (Playlist Import)
- The file path in the code doesn't match your song's file path
- File paths must match exactly
- Check that you have the same songs scanned

### "Invalid playlist format"
- The code doesn't start with `LOCATEDMUSIC_PLAYLIST_V1`
- Make sure you copied the entire code
- Check for extra spaces or characters

### Areas Not Importing
- Area names must match exactly (case-insensitive)
- Check `/xllog` for warnings about which areas couldn't be found
- Use the area search function to find exact names

### Times Not Importing
- Must be exactly: `Morning`, `Midday`, or `Night`
- Case doesn't matter, but spelling does
- Check `/xllog` for warnings

---

**Tip:** Save exported codes in a text file as backups! 📝

