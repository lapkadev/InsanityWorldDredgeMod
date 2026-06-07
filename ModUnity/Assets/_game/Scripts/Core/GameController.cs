using UnityEngine;
using static InsanityWorldMod.Core.Funcs;

namespace InsanityWorldMod.Core
{
    /// <summary>
    /// Public facade for the Core assembly. Api calls into here to drive Core's lifecycle.
    /// </summary>
    public static class GameController
    {
        // UI host references - destroyed and respawned on each OnGameLoaded.
        // Tracking them prevents duplicate MonoBehaviour subscribers 
        // (each new instance would re-subscribe to OnToggleSettings, causing N injected buttons after N reloads).
        private static GameObject _debugUiHost;
        private static GameObject _pauseButtonHost;
        private static GameObject _minimapWidgetHost;

        /// <summary>
        /// Initializes Core state to defaults. Called from `Api.EntrySystem.OnLoad()` after setup all hooks.
        /// </summary>
        public static void InitializeState()
        {
            G.Save = new SaveState();
            G.Game = new GameState();
            G.Run  = new RunState();
            G.Log.Info("GameController: state initialized");
        }

        /// <summary>
        /// Called when DREDGE finishes loading a save 
        /// </summary>
        public static void OnGameLoaded()
        {
            // Reset transient operation flags that may have been left stuck if the player
            // exited to main menu mid-operation (e.g. _isTeleporting in Funcs).
            Funcs.ResetTransientState();

            if (_debugUiHost != null)       Object.Destroy(_debugUiHost);
            if (_pauseButtonHost != null)   Object.Destroy(_pauseButtonHost);
            if (_minimapWidgetHost != null) Object.Destroy(_minimapWidgetHost);

            _debugUiHost = new GameObject("InsanityDebugRestartUI");
            _debugUiHost.AddComponent<DebugRestartUI>();
            Object.DontDestroyOnLoad(_debugUiHost);

            _pauseButtonHost = new GameObject("InsanityPauseMenuRestartButton");
            _pauseButtonHost.AddComponent<PauseMenuRestartButton>();
            Object.DontDestroyOnLoad(_pauseButtonHost);

            _minimapWidgetHost = new GameObject("InsanityMinimapWidget");
            _minimapWidgetHost.AddComponent<MinimapWidget>();
            Object.DontDestroyOnLoad(_minimapWidgetHost);

            Load("last");
            StartNewRun();

            G.Log.Info("GameController: OnGameLoaded done");
        }
    }
}
