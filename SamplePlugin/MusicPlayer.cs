using System;
using System.IO;
using System.Linq;
using NAudio.Wave;
using Dalamud.Plugin.Services;
using Lumina.Excel.Sheets;

namespace LocatedMusic;

public class MusicPlayer : IDisposable
{
    private readonly IPluginLog _log;
    private readonly Configuration _config;
    private WaveOutEvent? _waveOut;
    private AudioFileReader? _audioFile;
    private string? _currentSongPath;
    private bool _isDisposed = false;
    private LocationTracker? _locationTracker;
    private bool _isStopping = false; // Prevent concurrent stop operations
    
    // Fade state
    private enum FadeState
    {
        None,
        FadingIn,
        FadingOut
    }
    private FadeState _fadeState = FadeState.None;
    private DateTime _fadeStartTime;
    private float _fadeStartVolume;
    private float _fadeTargetVolume;

    public MusicPlayer(IPluginLog log, Configuration config)
    {
        _log = log;
        _config = config;
    }

    public void SetLocationTracker(LocationTracker locationTracker, bool isCombatPlayer)
    {
        _locationTracker = locationTracker;
        _isCombatPlayer = isCombatPlayer;
    }
    
    private bool _isCombatPlayer = false;

    public bool IsPlaying => _waveOut?.PlaybackState == PlaybackState.Playing;
    public bool IsPaused => _waveOut?.PlaybackState == PlaybackState.Paused;
    
    public TimeSpan? CurrentPosition => _audioFile?.CurrentTime;

    public void PlaySong(string filePath, TimeSpan? resumePosition = null)
    {
        // Always check if enabled first and stop if disabled
        if (!_config.Enabled)
        {
            Stop();
            return;
        }

        if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
        {
            return;
        }

        // If same song is already playing, don't restart
        if (_currentSongPath == filePath && IsPlaying)
        {
            return;
        }

        try
        {
            // Stop any currently playing music first
            Stop();

            // Double-check enabled state after stopping (might have been disabled)
            if (!_config.Enabled)
            {
                return;
            }

            _currentSongPath = filePath;
            _audioFile = new AudioFileReader(filePath)
            {
                Volume = _config.MasterVolume
            };
            
            // Resume from saved position if provided
            if (resumePosition.HasValue && resumePosition.Value < _audioFile.TotalTime)
            {
                _audioFile.CurrentTime = resumePosition.Value;
                _log.Information($"Resuming {Path.GetFileName(filePath)} from {resumePosition.Value.ToString(@"mm\:ss")}");
            }
            
            _waveOut = new WaveOutEvent();
            _waveOut.Init(_audioFile);
            _waveOut.PlaybackStopped += OnPlaybackStopped;
            
            // Start with volume 0 for fade in
            _audioFile.Volume = 0.0f;
            _waveOut.Play();
            
            // Start fade in
            StartFadeIn();

            _log.Information($"Now playing: {Path.GetFileName(filePath)}");
        }
        catch (Exception ex)
        {
            _log.Error(ex, $"Failed to play song: {filePath}");
            // Make sure to clean up on error
            Stop();
        }
    }

    public void Pause()
    {
        try
        {
            if (_waveOut != null && _waveOut.PlaybackState == PlaybackState.Playing)
            {
                // Start fade out, will pause when fade completes
                StartFadeOut();
            }
        }
        catch (Exception ex)
        {
            _log.Error(ex, "Error pausing music");
        }
    }

    public void Resume()
    {
        try
        {
            if (_waveOut != null && _waveOut.PlaybackState == PlaybackState.Paused)
            {
                // Set volume to 0 for fade in
                if (_audioFile != null)
                {
                    _audioFile.Volume = 0.0f;
                }
                
                // Resume playback first, then fade in
                _waveOut.Play();
                StartFadeIn();
                _log.Debug("Music resuming with fade in");
            }
        }
        catch (Exception ex)
        {
            _log.Error(ex, "Error resuming music");
        }
    }

    private void StartFadeIn()
    {
        if (_audioFile == null) return;
        
        _fadeStartVolume = _audioFile.Volume;
        _fadeTargetVolume = _config.MasterVolume;
        _fadeStartTime = DateTime.Now;
        _fadeState = FadeState.FadingIn;
    }

    private void StartFadeOut()
    {
        if (_audioFile == null) return;
        
        _fadeStartVolume = _audioFile.Volume;
        _fadeTargetVolume = 0.0f;
        _fadeStartTime = DateTime.Now;
        _fadeState = FadeState.FadingOut;
    }

    public TimeSpan? GetCurrentPosition()
    {
        return _audioFile?.CurrentTime;
    }

    public string? GetCurrentSongPath()
    {
        return _currentSongPath;
    }
    public (string? name, TimeSpan remaining, TimeSpan total) GetCurrentTrackInfo()
    {
        if (_audioFile == null || string.IsNullOrEmpty(_currentSongPath))
            return (null, TimeSpan.Zero, TimeSpan.Zero);

        try
        {
            var total = _audioFile.TotalTime;
            var current = _audioFile.CurrentTime;

            var remaining = total - current;
            if (remaining < TimeSpan.Zero)
                remaining = TimeSpan.Zero;

            var filename = Path.GetFileName(_currentSongPath);
            return (filename, remaining, total);
        }
        catch
        {
            return (null, TimeSpan.Zero, TimeSpan.Zero);
        }
    }

    public void Stop()
    {
        // Prevent concurrent stop operations
        if (_isStopping)
        {
            return;
        }

        _isStopping = true;
        try
        {
            // Reset fade state
            _fadeState = FadeState.None;
            
            // Unsubscribe from event first to prevent callbacks during disposal
            if (_waveOut != null)
            {
                _waveOut.PlaybackStopped -= OnPlaybackStopped;
                try
                {
                    _waveOut.Stop();
                }
                catch
                {
                    // Ignore errors when stopping
                }
                try
                {
                    _waveOut.Dispose();
                }
                catch
                {
                    // Ignore disposal errors
                }
                _waveOut = null;
            }

            if (_audioFile != null)
            {
                try
                {
                    _audioFile.Dispose();
                }
                catch
                {
                    // Ignore disposal errors
                }
                _audioFile = null;
            }

            _currentSongPath = null;
        }
        catch (Exception ex)
        {
            _log.Error(ex, "Error stopping music");
        }
        finally
        {
            _isStopping = false;
        }
    }

    public void SetVolume(float volume)
    {
        _config.MasterVolume = Math.Clamp(volume, 0.0f, 1.0f);
        if (_audioFile != null)
        {
            if (_fadeState == FadeState.None)
            {
                // No fade in progress, set volume directly
                _audioFile.Volume = _config.MasterVolume;
            }
            else if (_fadeState == FadeState.FadingIn)
            {
                // Update target volume for fade in
                _fadeTargetVolume = _config.MasterVolume;
            }
            // If fading out, don't change target (should fade to 0)
        }
    }

    public void Update()
    {
        if (_fadeState == FadeState.None || _audioFile == null)
        {
            return;
        }

        var elapsed = (DateTime.Now - _fadeStartTime).TotalMilliseconds;
        var fadeDuration = _config.FadeDurationMs;

        if (elapsed >= fadeDuration)
        {
            // Fade complete
            _audioFile.Volume = _fadeTargetVolume;
            _fadeState = FadeState.None;

            if (_fadeTargetVolume == 0.0f && _waveOut != null && _waveOut.PlaybackState == PlaybackState.Playing)
            {
                // Fade out complete, pause
                _waveOut.Pause();
                _log.Debug("Fade out complete, music paused");
            }
            else if (_fadeTargetVolume > 0.0f && _waveOut != null && _waveOut.PlaybackState == PlaybackState.Paused)
            {
                // Fade in complete, ensure playing
                _waveOut.Play();
                _log.Debug("Fade in complete, music playing");
            }
        }
        else
        {
            // Interpolate volume
            var progress = (float)(elapsed / fadeDuration);
            var currentVolume = _fadeStartVolume + (_fadeTargetVolume - _fadeStartVolume) * progress;
            _audioFile.Volume = Math.Clamp(currentVolume, 0.0f, _config.MasterVolume);
        }
    }

    private void OnPlaybackStopped(object? sender, StoppedEventArgs e)
    {
        // Only process if not disposed and still enabled
        if (_isDisposed || !_config.Enabled)
        {
            return;
        }

        // If playback stopped naturally (song ended), trigger location tracker to play next song
        // This creates a loop effect - when a song ends, it picks another matching song
        if (e.Exception == null && _locationTracker != null && _config.Enabled)
        {
            _log.Debug($"Song ended naturally ({(_isCombatPlayer ? "combat" : "non-combat")}), checking for next song to play");
            _locationTracker.PlayNextSong(_isCombatPlayer);
        }
    }

    public void Dispose()
    {
        if (_isDisposed) return;
        _isDisposed = true;

        Stop();
    }
}

