# New Features - LocatedMusic

## 1. Search Functionality for Area Selection

### How to Use
- When you click on an area dropdown (e.g., "Area 1", "Area 2"), you'll see a search box at the top
- Type any part of an area name to filter the list
- The search is **case-insensitive** and matches anywhere in the name
- Examples:
  - Type "thanalan" to find all Thanalan areas
  - Type "shroud" to find all Shroud areas
  - Type "west" to find all areas with "west" in the name

### Benefits
- Much faster to find areas when you have hundreds of territories
- No more scrolling through long lists
- Works with partial matches

## 2. Bulk Paste Assignment

### For Areas
1. In the "Area Assignments" section, you'll see a text box labeled "Bulk Assign Areas"
2. Paste or type area names, separated by:
   - Commas: `West Thanalan, East Thanalan, Central Thanalan`
   - Newlines (one per line):
     ```
     West Thanalan
     East Thanalan
     Central Thanalan
     ```
   - Or a mix of both
3. Click "Apply" button
4. The plugin will:
   - Find matching areas (exact match first, then partial match)
   - Add them to the song's area list
   - Skip areas that are already assigned
   - Show warnings for areas that couldn't be found

### For Times of Day
1. In the "Time of Day Assignments" section, you'll see a text box
2. Paste or type time names, separated by commas or newlines:
   - `Morning, Midday, Night`
   - Or one per line:
     ```
     Morning
     Midday
     Night
     ```
3. Click "Apply" button
4. Valid values: `Morning`, `Midday`, `Night` (case-insensitive)

### Tips
- You can copy a list of areas from a text file or spreadsheet
- Partial area names work (e.g., "Thanalan" will match "West Thanalan")
- If an area isn't found, check `/xllog` for the exact name
- The text box clears after applying

## 3. Song Looping

### How It Works
- When a song ends naturally (reaches the end), the plugin automatically:
  1. Checks for other songs that match your current location and time
  2. Randomly picks one and plays it
  3. This creates a continuous music loop

### Benefits
- No more silence after a song ends
- Continuous music as long as you have matching songs
- Random selection keeps it interesting
- Works automatically - no configuration needed

### Behavior
- If there are multiple matching songs, it randomly picks one (might repeat the same song)
- If there's only one matching song, it will loop that same song
- If no songs match when one ends, music stops (same as before)

## Usage Examples

### Example 1: Assign Multiple Areas Quickly
```
Bulk Assign Areas:
West Thanalan, East Thanalan, Central Thanalan, North Thanalan, South Thanalan
```
Click "Apply" → All 5 areas added instantly!

### Example 2: Assign from a List
Copy from a text file:
```
The Lavender Beds
The Goblet
Mist
Shirogane
Empyreum
```
Paste and click "Apply" → All housing areas added!

### Example 3: Assign All Times
```
Bulk Assign Times:
Morning, Midday, Night
```
Click "Apply" → Song plays at all times!

## Troubleshooting

### Bulk Assignment Not Working
- Check `/xllog` for warnings about areas that couldn't be found
- Make sure area names match exactly (or are close enough for partial match)
- Try one area at a time to see which ones work

### Search Not Finding Areas
- Make sure you're typing part of the area name correctly
- Search is case-insensitive, so "THANALAN" works the same as "thanalan"
- The search matches anywhere in the name, so "west" will find "West Thanalan"

### Songs Not Looping
- Make sure you have at least one song assigned to your current area and time
- Check that songs are valid MP3 files
- Check `/xllog` for errors when a song ends

---

**Enjoy the improved workflow!** 🎵

