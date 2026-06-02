using System.IO;
using System.Reflection;
using HarmonyLib;
using InsanityWorldMod.Core;
using UnityEngine;
using Winch.Core;
using Winch.Util;
using static InsanityWorldMod.Api.Constants;

namespace InsanityWorldMod.Api
{
    /// <summary>
    /// Api entry point. Called by the mod-repo Loader.cs (which Winch invokes).
    /// Wires Core-side delegates (G.Log, DredgeHooks), initializes Core state,
    /// spawns MainBehaviour, registers Harmony patches, subscribes to DREDGE events.
    /// </summary>
    public static class Entry
    {
        public static void Initialize()
        {
            // 1. Wire Core's logger delegates to Winch's logger.
            G.Log.Info  = msg => WinchCore.Log.Info(msg);
            G.Log.Warn  = msg => WinchCore.Log.Warn(msg);
            G.Log.Error = msg => WinchCore.Log.Error(msg);
            G.Log.Debug = msg => WinchCore.Log.Debug(msg);

            // 2. Wire DredgeHooks - DREDGE/Winch APIs that Core cannot reference directly.
            // ModAssemblyLoader.GetCurrentMod() returns null OUTSIDE the mod-load context
            // (i.e. after Initialize() finishes - most runtime call sites). Fall back to
            // this Api.dll's on-disk folder, which equals the mod folder under DREDGE/Mods/.
            var apiAssemblyDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            DredgeHooks.FindDockById   = id => DockUtil.GetDock(id);
            DredgeHooks.GetModBasePath = () => ModAssemblyLoader.GetCurrentMod()?.BasePath ?? apiAssemblyDir;

            G.Log.Info("Entry.Initialize: hooks wired");

            // 3. Initialize Core state
            GameController.InitializeState();

            // 4. Spawn MonoBehaviour host for periodic ticks (auto-save, Run.Tick).
            var mainGo = new GameObject(nameof(InsanityWorldMod));
            mainGo.AddComponent<MainBehaviour>();
            Object.DontDestroyOnLoad(mainGo);

            // 5. Register Harmony patches in this assembly (scans Api.dll for [HarmonyPatch]).
            new Harmony(HARMONY_ID).PatchAll(Assembly.GetExecutingAssembly());

            // 6. Subscribe to DREDGE save-loaded event - Core handles UI spawn + Load + StartNewRun.
            ApplicationEvents.Instance.OnGameLoaded += GameController.OnGameLoaded;

            G.Log.Info("Entry.Initialize: done");
        }
    }
}
