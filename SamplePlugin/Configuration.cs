using Dalamud.Configuration;
using System;
using System.Collections.Generic;

namespace LocatedMusic;

[Serializable]
public class Configuration : IPluginConfiguration
{
    public int Version { get; set; } = 0;

    // Music folder path
    public string MusicFolderPath { get; set; } = string.Empty;

    // List of songs with their area and time assignments
    public List<SongEntry> Songs { get; set; } = new();

    // Master volume (0.0 to 1.0)
    public float MasterVolume { get; set; } = 0.5f;

    // Enable/disable the plugin
    public bool Enabled { get; set; } = true;

    // Fade in/out duration in milliseconds
    public int FadeDurationMs { get; set; } = 1000;

    public void Save()
    {
        Plugin.PluginInterface.SavePluginConfig(this);
    }
}

[Serializable]
public class SongEntry
{
    // File path or Spotify track ID
    public string FilePath { get; set; } = string.Empty;
    
    // Display name
    public string Name { get; set; } = string.Empty;
    
    // Is this a Spotify track?
    public bool IsSpotifyTrack { get; set; } = false;
    
    // Spotify track ID (if applicable)
    public string SpotifyTrackId { get; set; } = string.Empty;
    
    // List of territory IDs where this song should play
    public List<uint> TerritoryIds { get; set; } = new();
    
    // List of time of day when this song should play (Morning, Midday, Night)
    public List<TimeOfDay> TimeOfDays { get; set; } = new();
    
    // If true, this song can play in any area (overrides territory list)
    public bool PlayInAnyArea { get; set; } = false;
    
    // If true, this song can play at any time (overrides time of day list)
    public bool PlayAtAnyTime { get; set; } = false;
    
    // If true, this song only plays during battle/combat. If false, it doesn't play during battle
    public bool PlayDuringBattle { get; set; } = false;
}

[Serializable]
public enum TimeOfDay
{
    Morning,
    Midday,
    Night
}
