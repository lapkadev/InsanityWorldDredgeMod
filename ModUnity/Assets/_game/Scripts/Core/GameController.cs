using UnityEngine;
using static InsanityWorldMod.Core.Funcs;

namespace InsanityWorldMod.Core
{
    public static partial class Constants
    {
        public const string PREFIX = "lapkadev_";
    }

    public static partial class G
    {
        public static RectTransform GameCanvas;
        public static string        ModBasePath;
    }

    /// <summary>
    /// Public facade for the Core assembly. Api calls into here to drive Core's lifecycle.
    /// </summary>
    public static class GameController
    {
        // UI host references - destroyed and respawned on each OnGameLoaded.
        // Tracking them prevents duplicate MonoBehaviour subscribers 
        // (each new instance would re-subscribe to OnToggleSettings, causing N injected buttons after N reloads).
        // private static GameObject _debugUiHost;
        // private static GameObject _pauseButtonHost;
        private static GameObject _minimapWidgetHost;
        private static GameObject _compassWidgetHost;

        /// <summary>
        /// Initializes Core state to defaults. Called from `Api.EntrySystem.OnLoad()` after setup all hooks.
        /// </summary>
        public static void InitializeState()
        {
            G.Save = new SaveState();
            G.Game = new GameState();
            G.Run  = new RunState();
            Log.Info("GameController: state initialized");
        }

        /// <summary>
        /// Called when DREDGE finishes loading a save
        /// </summary>
        public static void OnGameLoaded()
        {
            // Reset transient operation flags that may have been left stuck if the player
            // exited to main menu mid-operation (e.g. _isTeleporting in Funcs).
            ResetTransientState();
            InitKeyBindings();

            // if (_debugUiHost != null)
            //     Object.Destroy(_debugUiHost);

            // if (_pauseButtonHost != null)
            //     Object.Destroy(_pauseButtonHost);

            if (_minimapWidgetHost != null)
                Object.Destroy(_minimapWidgetHost);

            if (_compassWidgetHost != null)
                Object.Destroy(_compassWidgetHost);

            // _debugUiHost = new GameObject("InsanityDebugRestartUI");
            // _debugUiHost.AddComponent<DebugRestartUI>();
            // Object.DontDestroyOnLoad(_debugUiHost);

            // _pauseButtonHost = new GameObject("InsanityPauseMenuRestartButton");
            // _pauseButtonHost.AddComponent<PauseMenuRestartButton>();
            // Object.DontDestroyOnLoad(_pauseButtonHost);

            _minimapWidgetHost = new GameObject("InsanityMinimapWidget");
            _minimapWidgetHost.AddComponent<MinimapWidget>();
            Object.DontDestroyOnLoad(_minimapWidgetHost);

            _compassWidgetHost = CompassWidget.TryCreate();

            Load("last");
            StartNewRun();

            Log.Info("GameController: OnGameLoaded done");
        }
    }
}
