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
    /// First system to load (Order=0).
    /// Actions:
    /// - wires Core delegates
    /// - initializes G.* state,
    /// - spawns MainBehaviour,
    /// - registers Harmony,
    /// - subscribes to DREDGE events.
    /// </summary>
    public class EntrySystem : IInsanityWorldSystem
    {
        public int Order => 0;

        public void OnLoad()
        {
            G.Log.Info  = msg => WinchCore.Log.Info(msg);
            G.Log.Warn  = msg => WinchCore.Log.Warn(msg);
            G.Log.Error = msg => WinchCore.Log.Error(msg);
            G.Log.Debug = msg => WinchCore.Log.Debug(msg);

            var apiAssemblyDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            DredgeHooks.FindDockById   = id => DockUtil.GetDock(id);
            DredgeHooks.GetModBasePath = () => ModAssemblyLoader.GetCurrentMod()?.BasePath ?? apiAssemblyDir;
            DredgeHooks.GetAllBundles  = () => AssetBundleUtil.AssetBundles.Values;

            G.Log.Info("EntrySystem.OnLoad: hooks wired");

            GameController.InitializeState();

            var mainGo = new GameObject(nameof(InsanityWorldMod));
            mainGo.AddComponent<MainBehaviour>();
            Object.DontDestroyOnLoad(mainGo);

            new Harmony(HARMONY_ID).PatchAll(Assembly.GetExecutingAssembly());

            ApplicationEvents.Instance.OnGameLoaded += GameController.OnGameLoaded;

            G.Log.Info("EntrySystem.OnLoad: done");
        }
    }
}
