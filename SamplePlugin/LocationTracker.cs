using System;
using System.IO;
using System.Linq;
using Dalamud.Game.ClientState.Conditions;
using Dalamud.Plugin.Services;
using Lumina.Excel.Sheets;

namespace LocatedMusic;

public class LocationTracker
{
    private readonly IClientState _clientState;
    private readonly IDataManager _dataManager;
    private readonly IPluginLog _log;
    private readonly Configuration _config;
    private readonly MusicPlayer _nonCombatMusicPlayer;
    private readonly MusicPlayer _combatMusicPlayer;
    private readonly ICondition _condition;
    
    private uint _lastTerritoryId = 0;
    private TimeOfDay _lastTimeOfDay = TimeOfDay.Midday;
    private bool _hasInitialized = false;
    private bool _lastInCombat = false;
    
    // Track last songs and positions for combat/non-combat states
    private string? _lastCombatSongPath;
    private TimeSpan? _lastCombatSongPosition;
    private DateTime _lastCombatSongTime = DateTime.MinValue;
    
    private string? _lastNonCombatSongPath;
    private TimeSpan? _lastNonCombatSongPosition;
    private DateTime _lastNonCombatSongTime = DateTime.MinValue;
    
    // Delay timer for combat state changes - keep combat player alive for 15 seconds
    private DateTime? _combatExitTime;
    private const int CombatPlayerKeepAliveSeconds = 15;
    private const int ResumeWindowSeconds = 30;

    public LocationTracker(IClientState clientState, IDataManager dataManager, IPluginLog log, Configuration config, MusicPlayer nonCombatMusicPlayer, MusicPlayer combatMusicPlayer, ICondition condition)
    {
        _clientState = clientState;
        _dataManager = dataManager;
        _log = log;
        _config = config;
        _nonCombatMusicPlayer = nonCombatMusicPlayer;
        _combatMusicPlayer = combatMusicPlayer;
        _condition = condition;
    }

    public void Update()
    {
        if (!_config.Enabled)
        {
            // If disabled, make sure both players stop
            if (_hasInitialized)
            {
                _nonCombatMusicPlayer.Stop();
                _combatMusicPlayer.Stop();
                _hasInitialized = false;
            }
            return;
        }

        // Don't update if client isn't logged in
        if (!_clientState.IsLoggedIn)
        {
            return;
        }

        var currentTerritoryId = _clientState.TerritoryType;
        
        // Skip if territory is 0 (not loaded yet)
        if (currentTerritoryId == 0)
        {
            return;
        }
        
        var currentTimeOfDay = GetCurrentTimeOfDay();
        var currentInCombat = IsInCombat();

        // Handle combat state changes
        if (currentInCombat != _lastInCombat && _hasInitialized)
        {
            if (currentInCombat)
            {
                // Entering combat: pause non-combat music, start combat music
                if (_nonCombatMusicPlayer.IsPlaying)
                {
                    _nonCombatMusicPlayer.Pause();
                    SaveCurrentSongPosition(false);
                }
                
                // Clear combat exit timer
                _combatExitTime = null;
                
                // Start combat music
                PlayAppropriateSong(currentTerritoryId, currentTimeOfDay, true);
            }
            else
            {
                // Exiting combat: pause combat music, resume non-combat music
                if (_combatMusicPlayer.IsPlaying)
                {
                    _combatMusicPlayer.Pause();
                    SaveCurrentSongPosition(true);
                }
                
                // Start timer to keep combat player alive
                _combatExitTime = DateTime.Now;
                
                // Resume non-combat music if it was paused
                if (_nonCombatMusicPlayer.IsPaused)
                {
                    _nonCombatMusicPlayer.Resume();
                }
                else if (!_nonCombatMusicPlayer.IsPlaying)
                {
                    // Start non-combat music if not playing
                    PlayAppropriateSong(currentTerritoryId, currentTimeOfDay, false);
                }
            }
            
            _lastInCombat = currentInCombat;
        }

        // Check if we should dispose combat player (15 seconds after exiting combat)
        if (_combatExitTime.HasValue && !currentInCombat)
        {
            var timeSinceExit = (DateTime.Now - _combatExitTime.Value).TotalSeconds;
            if (timeSinceExit >= CombatPlayerKeepAliveSeconds)
            {
                // Stop combat player after delay
                if (_combatMusicPlayer.IsPlaying || _combatMusicPlayer.IsPaused)
                {
                    _combatMusicPlayer.Stop();
                }
                _combatExitTime = null;
            }
        }

        // Check if location or time changed
        if (!_hasInitialized || currentTerritoryId != _lastTerritoryId || currentTimeOfDay != _lastTimeOfDay)
        {
            _lastTerritoryId = currentTerritoryId;
            _lastTimeOfDay = currentTimeOfDay;
            if (!_hasInitialized)
            {
                _lastInCombat = currentInCombat;
            }
            _hasInitialized = true;

            // Update music for current combat state
            if (currentInCombat)
            {
                PlayAppropriateSong(currentTerritoryId, currentTimeOfDay, true);
            }
            else
            {
                PlayAppropriateSong(currentTerritoryId, currentTimeOfDay, false);
            }
        }
    }

    public void ForceUpdate()
    {
        _hasInitialized = false;
        Update();
    }

    private TimeOfDay GetCurrentTimeOfDay()
    {
        // FFXIV uses Eorzean time which is 70x faster than real time
        // Each Eorzean day = 175 Earth seconds (86400 / 70 / 7.04 ≈ 175)
        // Actually, it's simpler: Eorzean time advances 60 seconds per Earth second
        
        // Calculate Eorzean time in seconds since epoch
        // Eorzean epoch: January 1, 1970 00:00:00 UTC
        var earthSeconds = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var eorzeanSeconds = (earthSeconds * 60) % 86400; // 86400 = seconds in an Eorzean day
        var eorzeanHour = (int)(eorzeanSeconds / 3600);

        // Morning: 5:00 - 11:59 (5-11)
        // Midday: 12:00 - 17:59 (12-17)
        // Night: 18:00 - 4:59 (18-23, 0-4)
        if (eorzeanHour >= 5 && eorzeanHour < 12)
        {
            return TimeOfDay.Morning;
        }
        else if (eorzeanHour >= 12 && eorzeanHour < 18)
        {
            return TimeOfDay.Midday;
        }
        else
        {
            return TimeOfDay.Night;
        }
    }

    private bool IsInCombat()
    {
        return _condition[ConditionFlag.InCombat];
    }

    private void SaveCurrentSongPosition(bool wasInCombat)
    {
        var player = wasInCombat ? _combatMusicPlayer : _nonCombatMusicPlayer;
        var currentPath = player.GetCurrentSongPath();
        var currentPosition = player.GetCurrentPosition();
        
        if (!string.IsNullOrEmpty(currentPath) && currentPosition.HasValue)
        {
            if (wasInCombat)
            {
                _lastCombatSongPath = currentPath;
                _lastCombatSongPosition = currentPosition;
                _lastCombatSongTime = DateTime.Now;
                _log.Debug($"Saved combat song position: {Path.GetFileName(currentPath)} at {currentPosition.Value.ToString(@"mm\:ss")}");
            }
            else
            {
                _lastNonCombatSongPath = currentPath;
                _lastNonCombatSongPosition = currentPosition;
                _lastNonCombatSongTime = DateTime.Now;
                _log.Debug($"Saved non-combat song position: {Path.GetFileName(currentPath)} at {currentPosition.Value.ToString(@"mm\:ss")}");
            }
        }
    }

    private void PlayAppropriateSong(uint territoryId, TimeOfDay timeOfDay, bool inCombat)
    {
        var player = inCombat ? _combatMusicPlayer : _nonCombatMusicPlayer;
        var playerType = inCombat ? "combat" : "non-combat";
        
        _log.Debug($"Checking {playerType} songs for territory {territoryId} at {timeOfDay}. Total songs: {_config.Songs.Count}");
        
        // Check if we should resume a previous song
        string? resumePath = null;
        TimeSpan? resumePosition = null;
        
        if (inCombat && _lastCombatSongPath != null)
        {
            var timeSinceLast = (DateTime.Now - _lastCombatSongTime).TotalSeconds;
            if (timeSinceLast <= ResumeWindowSeconds)
            {
                resumePath = _lastCombatSongPath;
                resumePosition = _lastCombatSongPosition;
                _log.Debug($"Will resume combat song: {Path.GetFileName(resumePath)} from {resumePosition?.ToString(@"mm\:ss")}");
            }
        }
        else if (!inCombat && _lastNonCombatSongPath != null)
        {
            var timeSinceLast = (DateTime.Now - _lastNonCombatSongTime).TotalSeconds;
            if (timeSinceLast <= ResumeWindowSeconds)
            {
                resumePath = _lastNonCombatSongPath;
                resumePosition = _lastNonCombatSongPosition;
                _log.Debug($"Will resume non-combat song: {Path.GetFileName(resumePath)} from {resumePosition?.ToString(@"mm\:ss")}");
            }
        }
        
        // Find songs that match current location, time, and combat state
        var matchingSongs = _config.Songs.Where(song =>
        {
            // Check area match
            bool areaMatches = song.PlayInAnyArea || song.TerritoryIds.Contains(territoryId);
            
            // Check time match
            bool timeMatches = song.PlayAtAnyTime || song.TimeOfDays.Contains(timeOfDay);
            
            // Check battle match: if PlayDuringBattle is true, only play during combat
            // If PlayDuringBattle is false, only play when NOT in combat
            bool battleMatches = song.PlayDuringBattle == inCombat;
            
            if (areaMatches && timeMatches && battleMatches)
            {
                _log.Debug($"Song matches: {song.Name} (PlayInAnyArea: {song.PlayInAnyArea}, PlayAtAnyTime: {song.PlayAtAnyTime}, PlayDuringBattle: {song.PlayDuringBattle}, Territories: {song.TerritoryIds.Count}, Times: {song.TimeOfDays.Count})");
            }
            
            return areaMatches && timeMatches && battleMatches;
        }).ToList();

        if (matchingSongs.Count == 0)
        {
            _log.Warning($"No matching {playerType} songs found for territory {territoryId} at {timeOfDay}. Check your song assignments!");
            player.Stop();
            return;
        }

        // Try to resume previous song if it matches
        SongEntry? selectedSong = null;
        if (resumePath != null)
        {
            selectedSong = matchingSongs.FirstOrDefault(s => s.FilePath == resumePath);
            if (selectedSong != null)
            {
                _log.Information($"Resuming previous {playerType} song: {selectedSong.Name}");
            }
        }
        
        // If no resume match, pick a random song
        if (selectedSong == null)
        {
            var random = new Random();
            selectedSong = matchingSongs[random.Next(matchingSongs.Count)];
            resumePosition = null; // Don't resume if it's a different song
            _log.Information($"Selected new {playerType} song: {selectedSong.Name}");
        }

        // For now, only support MP3 files (Spotify integration would require additional setup)
        if (selectedSong.IsSpotifyTrack)
        {
            _log.Warning("Spotify integration not yet implemented. Skipping Spotify track.");
            return;
        }

        if (string.IsNullOrEmpty(selectedSong.FilePath))
        {
            _log.Warning($"Song has no file path: {selectedSong.Name}");
            return;
        }

        if (!File.Exists(selectedSong.FilePath))
        {
            _log.Warning($"Song file not found: {selectedSong.FilePath}");
            return;
        }

        _log.Information($"Attempting to play {playerType} song: {selectedSong.FilePath}");
        player.PlaySong(selectedSong.FilePath, resumePosition);
        
        // Update saved song info
        if (inCombat)
        {
            _lastCombatSongPath = selectedSong.FilePath;
            _lastCombatSongTime = DateTime.Now;
        }
        else
        {
            _lastNonCombatSongPath = selectedSong.FilePath;
            _lastNonCombatSongTime = DateTime.Now;
        }
    }

    public string? GetCurrentAreaName()
    {
        var territoryId = _clientState.TerritoryType;
        if (_dataManager.GetExcelSheet<TerritoryType>().TryGetRow(territoryId, out var territoryRow))
        {
            try
            {
                var placeName = territoryRow.PlaceName.Value;
                return placeName.Name.ToString();
            }
            catch
            {
                // PlaceName is invalid or doesn't exist
                return null;
            }
        }
        return null;
    }

    public uint GetCurrentTerritoryId()
    {
        return _clientState.TerritoryType;
    }

    public TimeOfDay GetCurrentTimeOfDayPublic()
    {
        return GetCurrentTimeOfDay();
    }

    public bool IsInCombatPublic()
    {
        return IsInCombat();
    }

    public void PlayNextSong(bool isCombat)
    {
        // Only play next song if plugin is still enabled
        if (!_config.Enabled)
        {
            return;
        }

        // Called when a song ends - play another matching song to create a loop effect
        var currentTerritoryId = _clientState.TerritoryType;
        var currentTimeOfDay = GetCurrentTimeOfDay();
        var currentInCombat = IsInCombat();
        
        // Only play next song if we're in the correct combat state
        if (currentTerritoryId != 0 && currentInCombat == isCombat)
        {
            PlayAppropriateSong(currentTerritoryId, currentTimeOfDay, isCombat);
        }
    }
}
