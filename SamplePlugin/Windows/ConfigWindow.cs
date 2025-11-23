using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Windows.Forms;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Interface.Windowing;
using Dalamud.Plugin.Services;
using Lumina.Excel.Sheets;

namespace LocatedMusic.Windows;

public class ConfigWindow : Window, IDisposable
{
    private readonly Plugin _plugin;
    private string _musicFolderPath = string.Empty;
    private string _selectedSongName = string.Empty;
    private int _selectedSongIndex = -1;
    private List<TerritoryInfo> _allTerritories = new();
    private bool _territoriesLoaded = false;
    private string _areaSearchFilter = string.Empty;
    private string _bulkTimeText = string.Empty;

    public ConfigWindow(Plugin plugin) : base("LocatedMusic Configuration###LocatedMusicConfig")
    {
        Flags = ImGuiWindowFlags.None; // Enable scrolling

        Size = new Vector2(900, 600);
        SizeCondition = ImGuiCond.FirstUseEver;

        _plugin = plugin;
        _musicFolderPath = plugin.Configuration.MusicFolderPath;
    }

    public void Dispose() { }

    private void LoadTerritories()
    {
        if (_territoriesLoaded) return;

        try
        {
            var territorySheet = LocatedMusic.Plugin.DataManager.GetExcelSheet<TerritoryType>();
            if (territorySheet != null)
            {
                _allTerritories = new List<TerritoryInfo>();
                foreach (var territory in territorySheet)
                {
                    try
                    {
                        var placeName = territory.PlaceName.Value;
                        var name = placeName.Name.ToString();
                        if (!string.IsNullOrEmpty(name))
                        {
                            _allTerritories.Add(new TerritoryInfo
                            {
                                Id = territory.RowId,
                                Name = name
                            });
                        }
                    }
                    catch
                    {
                        // Skip invalid territories (PlaceName doesn't exist or is invalid)
                        continue;
                    }
                }
                _allTerritories = _allTerritories.OrderBy(t => t.Name).ToList();

                _territoriesLoaded = true;
            }
        }
        catch (Exception ex)
        {
            Plugin.Log.Error(ex, "Failed to load territories");
        }
    }

    public override void Draw()
    {
        LoadTerritories();

        // Master controls
        ImGui.Text("Master Controls");
        ImGui.Separator();

        var enabled = _plugin.Configuration.Enabled;
        if (ImGui.Checkbox("Enable LocatedMusic", ref enabled))
        {
            _plugin.Configuration.Enabled = enabled;
            _plugin.Configuration.Save();
            if (!enabled)
            {
                _plugin.NonCombatMusicPlayer.Stop();
                _plugin.CombatMusicPlayer.Stop();
            }
        }

        ImGui.SameLine();
        var volume = _plugin.Configuration.MasterVolume;
        if (ImGui.SliderFloat("Volume", ref volume, 0.0f, 1.0f))
        {
            _plugin.Configuration.MasterVolume = volume;
            _plugin.Configuration.Save();
            _plugin.NonCombatMusicPlayer.SetVolume(volume);
            _plugin.CombatMusicPlayer.SetVolume(volume);
        }

        ImGui.Spacing();

        // Music folder selection
        ImGui.Text("Music Folder");
        ImGui.Separator();
        ImGui.InputText("Folder Path", ref _musicFolderPath, 500);
        ImGui.SameLine();
        if (ImGui.Button("Browse..."))
        {
            // Note: Dalamud doesn't have a built-in folder picker, so user needs to paste path manually
            // Or we could use Windows Forms, but that's more complex
        }
        ImGui.SameLine();
        if (ImGui.Button("Scan Folder"))
        {
            ScanMusicFolder();
        }

        if (_musicFolderPath != _plugin.Configuration.MusicFolderPath)
        {
            _plugin.Configuration.MusicFolderPath = _musicFolderPath;
            _plugin.Configuration.Save();
        }

        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();

        // Current status
        ImGui.Text("Current Status");
        ImGui.Separator();
        var areaName = _plugin.LocationTracker.GetCurrentAreaName() ?? "Unknown";
        var timeOfDay = _plugin.LocationTracker.GetCurrentTimeOfDayPublic();
        var territoryId = _plugin.LocationTracker.GetCurrentTerritoryId();
        var inCombat = _plugin.LocationTracker.IsInCombatPublic();
        ImGui.Text($"Location: {areaName} (ID: {territoryId})");
        ImGui.Text($"Time of Day: {timeOfDay}");
        ImGui.Text($"In Combat: {(inCombat ? "Yes" : "No")}");
        var nonCombatPlaying = _plugin.NonCombatMusicPlayer.IsPlaying || _plugin.NonCombatMusicPlayer.IsPaused;
        var combatPlaying = _plugin.CombatMusicPlayer.IsPlaying || _plugin.CombatMusicPlayer.IsPaused;
        ImGui.Text($"Non-Combat Music: {(nonCombatPlaying ? (_plugin.NonCombatMusicPlayer.IsPaused ? "Paused" : "Playing") : "Stopped")}");
        ImGui.Text($"Combat Music: {(combatPlaying ? (_plugin.CombatMusicPlayer.IsPaused ? "Paused" : "Playing") : "Stopped")}");
        ImGui.SameLine();
        if (ImGui.SmallButton("Force Check"))
        {
            _plugin.LocationTracker.ForceUpdate();
        }
        ImGui.SameLine();
        if (ImGui.SmallButton("Stop All"))
        {
            _plugin.NonCombatMusicPlayer.Stop();
            _plugin.CombatMusicPlayer.Stop();
        }

        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();

        // Song list and configuration
        ImGui.Text("Song Configuration");
        ImGui.SameLine();
        if (ImGui.Button("Export All##ExportAll"))
        {
            ExportAllSongs();
        }
        ImGui.SameLine();
        if (ImGui.Button("Import All##ImportAll"))
        {
            ImportAllSongs();
        }
        ImGui.SameLine();
        if (ImGui.Button("Clear##ClearAll"))
        {
            ClearAllSongs();
        }
        ImGui.Separator();

        using (var child = ImRaii.Child("SongList", new Vector2(ImGui.GetContentRegionAvail().X, 300), true))
        {
            if (child.Success)
            {
                DrawSongList();
            }
        }

        ImGui.Spacing();

        // Song details editor
        if (_selectedSongIndex >= 0 && _selectedSongIndex < _plugin.Configuration.Songs.Count)
        {
            DrawSongEditor(_plugin.Configuration.Songs[_selectedSongIndex]);
        }
    }

    private void ScanMusicFolder()
    {
        if (string.IsNullOrEmpty(_musicFolderPath) || !Directory.Exists(_musicFolderPath))
        {
            return;
        }

        var mp3Files = Directory.GetFiles(_musicFolderPath, "*.mp3", SearchOption.AllDirectories);

        foreach (var file in mp3Files)
        {
            var fileName = Path.GetFileNameWithoutExtension(file);

            // Check if song already exists
            if (!_plugin.Configuration.Songs.Any(s => s.FilePath == file))
            {
                _plugin.Configuration.Songs.Add(new SongEntry
                {
                    FilePath = file,
                    Name = fileName,
                    IsSpotifyTrack = false
                });
            }
        }

        _plugin.Configuration.Save();
    }

    private void DrawSongList()
    {
        // Iterate backwards to safely remove items
        for (int i = _plugin.Configuration.Songs.Count - 1; i >= 0; i--)
        {
            var song = _plugin.Configuration.Songs[i];
            var isSelected = _selectedSongIndex == i;

            var displayName = string.IsNullOrEmpty(song.Name) ? Path.GetFileName(song.FilePath) : song.Name;

            // Delete button first (on the right)
            ImGui.PushID($"delete{i}");
            bool shouldDelete = false;
            if (ImGui.SmallButton("X"))
            {
                shouldDelete = true;
            }
            ImGui.PopID();

            ImGui.SameLine();

            // Selectable
            if (ImGui.Selectable($"{displayName}##song{i}", isSelected))
            {
                _selectedSongIndex = i;
                _selectedSongName = displayName;
            }

            // Remove song if delete was clicked
            if (shouldDelete)
            {
                _plugin.Configuration.Songs.RemoveAt(i);
                _plugin.Configuration.Save();
                if (_selectedSongIndex == i)
                {
                    _selectedSongIndex = -1;
                }
                else if (_selectedSongIndex > i)
                {
                    _selectedSongIndex--;
                }
            }
        }

        if (_plugin.Configuration.Songs.Count == 0)
        {
            ImGui.Text("No songs configured. Scan a music folder to add songs.");
        }
    }

    private void DrawSongEditor(SongEntry song)
    {
        ImGui.Text("Song Editor");
        ImGui.Separator();

        // Song name
        var songName = song.Name;
        if (ImGui.InputText("Song Name", ref songName, 200))
        {
            song.Name = songName;
            _plugin.Configuration.Save();
        }

        ImGui.Text($"File: {song.FilePath}");

        ImGui.Spacing();

        // Area assignments
        ImGui.Text("Area Assignments");

        var playInAnyArea = song.PlayInAnyArea;
        if (ImGui.Checkbox("Play in Any Area", ref playInAnyArea))
        {
            song.PlayInAnyArea = playInAnyArea;
            _plugin.Configuration.Save();
        }

        // Export/Import buttons for this song
        ImGui.Spacing();
        if (ImGui.Button("Export Settings##ExportSong"))
        {
            ExportSongSettings(song);
        }
        ImGui.SameLine();
        if (ImGui.Button("Import Settings##ImportSong"))
        {
            ImportSongSettings(song);
        }
        ImGui.SameLine();
        if (ImGui.Button("Clear Settings##ClearSong"))
        {
            ClearSongSettings(song);
        }
        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();

        if (!playInAnyArea)
        {
            // List of assigned territories
            for (int i = 0; i < song.TerritoryIds.Count; i++)
            {
                var territoryId = song.TerritoryIds[i];
                var territoryName = _allTerritories.FirstOrDefault(t => t.Id == territoryId)?.Name ?? $"Territory {territoryId}";

                ImGui.PushID($"territory{i}");
                if (ImGui.BeginCombo($"Area {i + 1}", territoryName))
                {
                    // Search filter
                    ImGui.SetNextItemWidth(-1);
                    ImGui.InputTextWithHint("##AreaSearch", "Search areas...", ref _areaSearchFilter, 200);
                    ImGui.Separator();

                    // Filter territories based on search
                    var filteredTerritories = string.IsNullOrEmpty(_areaSearchFilter)
                        ? _allTerritories
                        : _allTerritories.Where(t => t.Name.Contains(_areaSearchFilter, StringComparison.OrdinalIgnoreCase)).ToList();

                    if (filteredTerritories.Count == 0)
                    {
                        ImGui.Text("No areas found");
                    }
                    else
                    {
                        foreach (var territory in filteredTerritories)
                        {
                            var isSelected = song.TerritoryIds.Contains(territory.Id);
                            if (ImGui.Selectable(territory.Name, isSelected))
                            {
                                song.TerritoryIds[i] = territory.Id;
                                _plugin.Configuration.Save();
                                ImGui.CloseCurrentPopup();
                            }
                        }
                    }
                    ImGui.EndCombo();
                }
                ImGui.SameLine();
                if (ImGui.Button("Remove"))
                {
                    song.TerritoryIds.RemoveAt(i);
                    _plugin.Configuration.Save();
                    i--;
                }
                ImGui.PopID();
            }

            // Add new area button
            if (ImGui.Button("+ Add Area"))
            {
                song.TerritoryIds.Add(0);
                _plugin.Configuration.Save();
            }
        }

        ImGui.Spacing();

        // Time of day assignments
        ImGui.Text("Time of Day Assignments");

        var playAtAnyTime = song.PlayAtAnyTime;
        if (ImGui.Checkbox("Play at Any Time", ref playAtAnyTime))
        {
            song.PlayAtAnyTime = playAtAnyTime;
            _plugin.Configuration.Save();
        }

        if (!playAtAnyTime)
        {
            // Bulk paste time assignment
            ImGui.Text("Bulk Assign Times (comma or newline separated: Morning, Midday, Night):");
            if (ImGui.InputTextMultiline("##BulkTimes", ref _bulkTimeText, 500, new Vector2(ImGui.GetContentRegionAvail().X, 60)))
            {
                // Text changed
            }
            ImGui.SameLine();
            if (ImGui.Button("Apply##BulkTimes"))
            {
                ApplyBulkTimes(song, _bulkTimeText);
                _bulkTimeText = string.Empty;
            }
            ImGui.Spacing();

            // List of assigned times
            for (int i = 0; i < song.TimeOfDays.Count; i++)
            {
                var timeOfDay = song.TimeOfDays[i];
                var timeString = timeOfDay.ToString();

                ImGui.PushID($"time{i}");
                if (ImGui.BeginCombo($"Time {i + 1}", timeString))
                {
                    foreach (TimeOfDay time in Enum.GetValues<TimeOfDay>())
                    {
                        var isSelected = song.TimeOfDays.Contains(time);
                        if (ImGui.Selectable(time.ToString(), isSelected))
                        {
                            song.TimeOfDays[i] = time;
                            _plugin.Configuration.Save();
                        }
                    }
                    ImGui.EndCombo();
                }
                ImGui.SameLine();
                if (ImGui.Button("Remove"))
                {
                    song.TimeOfDays.RemoveAt(i);
                    _plugin.Configuration.Save();
                    i--;
                }
                ImGui.PopID();
            }

            // Add new time button
            if (ImGui.Button("+ Add Time of Day"))
            {
                song.TimeOfDays.Add(TimeOfDay.Midday);
                _plugin.Configuration.Save();
            }
        }

        // Battle condition
        ImGui.Spacing();
        ImGui.Text("Battle Condition");
        ImGui.Separator();
        var playDuringBattle = song.PlayDuringBattle;
        if (ImGui.Checkbox("Play During Battle", ref playDuringBattle))
        {
            song.PlayDuringBattle = playDuringBattle;
            _plugin.Configuration.Save();
        }
        ImGui.TextWrapped("If checked, this song only plays during combat. If unchecked, it doesn't play during combat.");
        ImGui.Spacing();
    }

    private void ExportSongSettings(SongEntry song)
    {
        try
        {
            // Format: AREAS:area1,area2|TIMES:Morning,Midday|PLAYINANYAREA:true|PLAYATANYTIME:false|PLAYDURINGBATTLE:false
            var areaNames = song.TerritoryIds
                .Select(id => _allTerritories.FirstOrDefault(t => t.Id == id)?.Name ?? $"Territory{id}")
                .ToList();

            var exportText = $"AREAS:{string.Join(",", areaNames)}|" +
                            $"TIMES:{string.Join(",", song.TimeOfDays)}|" +
                            $"PLAYINANYAREA:{song.PlayInAnyArea}|" +
                            $"PLAYATANYTIME:{song.PlayAtAnyTime}|" +
                            $"PLAYDURINGBATTLE:{song.PlayDuringBattle}";

            // Copy to clipboard (Dalamud doesn't have direct clipboard access, so we'll use Windows API)
            SetClipboardText(exportText);
            Plugin.Log.Information($"Exported settings for song: {song.Name}");
        }
        catch (Exception ex)
        {
            Plugin.Log.Error(ex, "Failed to export song settings");
        }
    }

    private void ImportSongSettings(SongEntry song)
    {
        try
        {
            var clipboardText = GetClipboardText();
            if (string.IsNullOrWhiteSpace(clipboardText))
            {
                Plugin.Log.Warning("Clipboard is empty");
                return;
            }

            // Parse format: AREAS:area1,area2|TIMES:Morning,Midday|PLAYINANYAREA:true|PLAYATANYTIME:false
            var parts = clipboardText.Split('|');
            var areas = new List<string>();
            var times = new List<string>();
            bool playInAnyArea = false;
            bool playAtAnyTime = false;
            bool playDuringBattle = false; // Default to false if not present

            foreach (var part in parts)
            {
                if (part.StartsWith("AREAS:"))
                {
                    var areaList = part.Substring(6);
                    if (!string.IsNullOrEmpty(areaList))
                    {
                        areas = areaList.Split(',').Select(a => a.Trim()).Where(a => !string.IsNullOrEmpty(a)).ToList();
                    }
                }
                else if (part.StartsWith("TIMES:"))
                {
                    var timeList = part.Substring(6);
                    if (!string.IsNullOrEmpty(timeList))
                    {
                        times = timeList.Split(',').Select(t => t.Trim()).Where(t => !string.IsNullOrEmpty(t)).ToList();
                    }
                }
                else if (part.StartsWith("PLAYINANYAREA:"))
                {
                    bool.TryParse(part.Substring(14), out playInAnyArea);
                }
                else if (part.StartsWith("PLAYATANYTIME:"))
                {
                    bool.TryParse(part.Substring(14), out playAtAnyTime);
                }
                else if (part.StartsWith("PLAYDURINGBATTLE:"))
                {
                    bool.TryParse(part.Substring(17), out playDuringBattle);
                }
            }

            // Apply settings
            song.PlayInAnyArea = playInAnyArea;
            song.PlayAtAnyTime = playAtAnyTime;
            song.PlayDuringBattle = playDuringBattle;
            song.TerritoryIds.Clear();
            song.TimeOfDays.Clear();

            // Add areas
            foreach (var areaName in areas)
            {
                var territory = _allTerritories.FirstOrDefault(t =>
                    t.Name.Equals(areaName, StringComparison.OrdinalIgnoreCase));

                if (territory == null)
                {
                    territory = _allTerritories.FirstOrDefault(t =>
                        t.Name.Contains(areaName, StringComparison.OrdinalIgnoreCase));
                }

                if (territory != null && !song.TerritoryIds.Contains(territory.Id))
                {
                    song.TerritoryIds.Add(territory.Id);
                }
            }

            // Add times
            foreach (var timeName in times)
            {
                if (Enum.TryParse<TimeOfDay>(timeName, true, out var timeOfDay))
                {
                    if (!song.TimeOfDays.Contains(timeOfDay))
                    {
                        song.TimeOfDays.Add(timeOfDay);
                    }
                }
            }

            _plugin.Configuration.Save();
            Plugin.Log.Information($"Imported settings for song: {song.Name}");
        }
        catch (Exception ex)
        {
            Plugin.Log.Error(ex, "Failed to import song settings");
        }
    }

    private void ExportAllSongs()
    {
        try
        {
            var exportLines = new List<string>();
            exportLines.Add("LOCATEDMUSIC_PLAYLIST_V1");

            foreach (var song in _plugin.Configuration.Songs)
            {
                var areaNames = song.TerritoryIds
                    .Select(id => _allTerritories.FirstOrDefault(t => t.Id == id)?.Name ?? $"Territory{id}")
                    .ToList();

                // Use just the filename instead of full path
                var fileName = Path.GetFileName(song.FilePath);

                var line = $"SONG:{fileName}|" +
                          $"AREAS:{string.Join(",", areaNames)}|" +
                          $"TIMES:{string.Join(",", song.TimeOfDays)}|" +
                          $"PLAYINANYAREA:{song.PlayInAnyArea}|" +
                          $"PLAYATANYTIME:{song.PlayAtAnyTime}|" +
                          $"PLAYDURINGBATTLE:{song.PlayDuringBattle}";

                exportLines.Add(line);
            }

            var exportText = string.Join("\n", exportLines);
            SetClipboardText(exportText);
            Plugin.Log.Information($"Exported playlist with {_plugin.Configuration.Songs.Count} songs");
        }
        catch (Exception ex)
        {
            Plugin.Log.Error(ex, "Failed to export playlist");
        }
    }

    private void ImportAllSongs()
    {
        try
        {
            var clipboardText = GetClipboardText();
            if (string.IsNullOrWhiteSpace(clipboardText))
            {
                Plugin.Log.Warning("Clipboard is empty");
                return;
            }

            var lines = clipboardText.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
            if (lines.Length == 0)
            {
                Plugin.Log.Warning("Clipboard text is invalid");
                return;
            }

            // Check version
            if (lines[0] != "LOCATEDMUSIC_PLAYLIST_V1")
            {
                Plugin.Log.Warning("Invalid playlist format");
                return;
            }

            var importedCount = 0;
            for (int i = 1; i < lines.Length; i++)
            {
                var line = lines[i];
                if (!line.StartsWith("SONG:"))
                    continue;

                var parts = line.Split('|');
                if (parts.Length < 2)
                    continue;

                var fileName = parts[0].Substring(5); // Remove "SONG:" - now contains just filename

                // Find matching song by filename (not full path)
                var song = _plugin.Configuration.Songs.FirstOrDefault(s =>
                    Path.GetFileName(s.FilePath).Equals(fileName, StringComparison.OrdinalIgnoreCase));
                if (song == null)
                {
                    Plugin.Log.Debug($"Song not found: {fileName}, skipping");
                    continue;
                }

                // Parse settings
                var areas = new List<string>();
                var times = new List<string>();
                bool playInAnyArea = false;
                bool playAtAnyTime = false;
                bool playDuringBattle = false; // Default to false if not present

                foreach (var part in parts.Skip(1))
                {
                    if (part.StartsWith("AREAS:"))
                    {
                        var areaList = part.Substring(6);
                        if (!string.IsNullOrEmpty(areaList))
                        {
                            areas = areaList.Split(',').Select(a => a.Trim()).Where(a => !string.IsNullOrEmpty(a)).ToList();
                        }
                    }
                    else if (part.StartsWith("TIMES:"))
                    {
                        var timeList = part.Substring(6);
                        if (!string.IsNullOrEmpty(timeList))
                        {
                            times = timeList.Split(',').Select(t => t.Trim()).Where(t => !string.IsNullOrEmpty(t)).ToList();
                        }
                    }
                    else if (part.StartsWith("PLAYINANYAREA:"))
                    {
                        bool.TryParse(part.Substring(14), out playInAnyArea);
                    }
                    else if (part.StartsWith("PLAYATANYTIME:"))
                    {
                        bool.TryParse(part.Substring(14), out playAtAnyTime);
                    }
                    else if (part.StartsWith("PLAYDURINGBATTLE:"))
                    {
                        bool.TryParse(part.Substring(17), out playDuringBattle);
                    }
                }

                // Apply settings
                song.PlayInAnyArea = playInAnyArea;
                song.PlayAtAnyTime = playAtAnyTime;
                song.PlayDuringBattle = playDuringBattle;
                song.TerritoryIds.Clear();
                song.TimeOfDays.Clear();

                // Add areas
                foreach (var areaName in areas)
                {
                    var territory = _allTerritories.FirstOrDefault(t =>
                        t.Name.Equals(areaName, StringComparison.OrdinalIgnoreCase));

                    if (territory == null)
                    {
                        territory = _allTerritories.FirstOrDefault(t =>
                            t.Name.Contains(areaName, StringComparison.OrdinalIgnoreCase));
                    }

                    if (territory != null && !song.TerritoryIds.Contains(territory.Id))
                    {
                        song.TerritoryIds.Add(territory.Id);
                    }
                }

                // Add times
                foreach (var timeName in times)
                {
                    if (Enum.TryParse<TimeOfDay>(timeName, true, out var timeOfDay))
                    {
                        if (!song.TimeOfDays.Contains(timeOfDay))
                        {
                            song.TimeOfDays.Add(timeOfDay);
                        }
                    }
                }

                importedCount++;
            }

            _plugin.Configuration.Save();
            Plugin.Log.Information($"Imported settings for {importedCount} songs");
        }
        catch (Exception ex)
        {
            Plugin.Log.Error(ex, "Failed to import playlist");
        }
    }

    private void SetClipboardText(string text)
    {
        try
        {
            System.Windows.Forms.Clipboard.SetText(text);
        }
        catch (Exception ex)
        {
            Plugin.Log.Error(ex, "Failed to set clipboard text");
        }
    }

    private string GetClipboardText()
    {
        try
        {
            return System.Windows.Forms.Clipboard.GetText();
        }
        catch (Exception ex)
        {
            Plugin.Log.Error(ex, "Failed to get clipboard text");
            return string.Empty;
        }
    }

    private void ClearAllSongs()
    {
        try
        {
            _plugin.Configuration.Songs.Clear();
            _plugin.Configuration.Save();
            _selectedSongIndex = -1;
            Plugin.Log.Information("Cleared all songs from configuration");
        }
        catch (Exception ex)
        {
            Plugin.Log.Error(ex, "Failed to clear all songs");
        }
    }

    private void ClearSongSettings(SongEntry song)
    {
        try
        {
            song.PlayInAnyArea = false;
            song.PlayAtAnyTime = false;
            song.PlayDuringBattle = false;
            song.TerritoryIds.Clear();
            song.TimeOfDays.Clear();
            _plugin.Configuration.Save();
            Plugin.Log.Information($"Cleared settings for song: {song.Name}");
        }
        catch (Exception ex)
        {
            Plugin.Log.Error(ex, "Failed to clear song settings");
        }
    }

    private void ApplyBulkTimes(SongEntry song, string bulkText)
    {
        if (string.IsNullOrWhiteSpace(bulkText))
            return;

        // Split by comma or newline
        var timeNames = bulkText.Split(new[] { ',', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(t => t.Trim())
            .Where(t => !string.IsNullOrEmpty(t))
            .ToList();

        var addedCount = 0;
        foreach (var timeName in timeNames)
        {
            // Try to parse as enum
            if (Enum.TryParse<TimeOfDay>(timeName, true, out var timeOfDay))
            {
                if (!song.TimeOfDays.Contains(timeOfDay))
                {
                    song.TimeOfDays.Add(timeOfDay);
                    addedCount++;
                }
            }
            else
            {
                Plugin.Log.Warning($"Invalid time of day: {timeName}. Valid values: Morning, Midday, Night");
            }
        }

        if (addedCount > 0)
        {
            _plugin.Configuration.Save();
            Plugin.Log.Information($"Added {addedCount} time(s) to song: {song.Name}");
        }
    }
}

public class TerritoryInfo
{
    public uint Id { get; set; }
    public string Name { get; set; } = string.Empty;
}
