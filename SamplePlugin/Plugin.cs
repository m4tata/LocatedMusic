using Dalamud.Game.Command;
using Dalamud.IoC;
using Dalamud.Plugin;
using Dalamud.Interface.Windowing;
using Dalamud.Plugin.Services;
using LocatedMusic.Windows;

namespace LocatedMusic;

public sealed class Plugin : IDalamudPlugin
{
    [PluginService] internal static IDalamudPluginInterface PluginInterface { get; private set; } = null!;
    [PluginService] internal static ICommandManager CommandManager { get; private set; } = null!;
    [PluginService] internal static IClientState ClientState { get; private set; } = null!;
    [PluginService] internal static IDataManager DataManager { get; private set; } = null!;
    [PluginService] internal static IPluginLog Log { get; private set; } = null!;
    [PluginService] internal static IFramework Framework { get; private set; } = null!;
    [PluginService] internal static ICondition Condition { get; private set; } = null!;

    private const string CommandName = "/locatedmusic";

    public Configuration Configuration { get; init; }
    public MusicPlayer NonCombatMusicPlayer { get; private set; } = null!;
    public MusicPlayer CombatMusicPlayer { get; private set; } = null!;
    public LocationTracker LocationTracker { get; private set; } = null!;

    public readonly WindowSystem WindowSystem = new("LocatedMusic");
    private ConfigWindow ConfigWindow { get; init; } = null!;

    public Plugin()
    {
        Configuration = PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();

        NonCombatMusicPlayer = new MusicPlayer(Log, Configuration);
        CombatMusicPlayer = new MusicPlayer(Log, Configuration);
        LocationTracker = new LocationTracker(ClientState, DataManager, Log, Configuration, NonCombatMusicPlayer, CombatMusicPlayer, Condition);
        NonCombatMusicPlayer.SetLocationTracker(LocationTracker, false);
        CombatMusicPlayer.SetLocationTracker(LocationTracker, true);

        ConfigWindow = new ConfigWindow(this);

        WindowSystem.AddWindow(ConfigWindow);

        CommandManager.AddHandler(CommandName, new CommandInfo(OnCommand)
        {
            HelpMessage = "Open LocatedMusic configuration window"
        });

        PluginInterface.UiBuilder.Draw += WindowSystem.Draw;
        PluginInterface.UiBuilder.OpenConfigUi += ToggleConfigUi;
        PluginInterface.UiBuilder.OpenMainUi += ToggleConfigUi; // Register main UI callback

        // Update location tracker every frame
        Framework.Update += OnFrameworkUpdate;

        Log.Information($"===LocatedMusic plugin loaded===");
    }

    private void OnFrameworkUpdate(IFramework framework)
    {
        LocationTracker.Update();
        NonCombatMusicPlayer.Update();
        CombatMusicPlayer.Update();
    }

    public void Dispose()
    {
        Framework.Update -= OnFrameworkUpdate;
        
        PluginInterface.UiBuilder.Draw -= WindowSystem.Draw;
        PluginInterface.UiBuilder.OpenConfigUi -= ToggleConfigUi;
        PluginInterface.UiBuilder.OpenMainUi -= ToggleConfigUi;
        
        WindowSystem.RemoveAllWindows();
        ConfigWindow.Dispose();

        NonCombatMusicPlayer.Dispose();
        CombatMusicPlayer.Dispose();
        LocationTracker = null!;

        CommandManager.RemoveHandler(CommandName);
    }

    private void OnCommand(string command, string args)
    {
        ConfigWindow.Toggle();
    }
    
    public void ToggleConfigUi() => ConfigWindow.Toggle();
}
