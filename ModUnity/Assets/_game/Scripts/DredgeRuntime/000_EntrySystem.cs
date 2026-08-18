using System.Reflection;
using HarmonyLib;
using InsanityWorldMod.Core;
using UnityEngine;
using static InsanityWorldMod.DredgeRuntime.Constants;
using static InsanityWorldMod.DredgeRuntime.Funcs;

namespace InsanityWorldMod.DredgeRuntime
{
    public static partial class Constants
    {
        public const string MOD_GUID   = "lapkadev.InsanityWorldMod";
        public const string HARMONY_ID = "lapkadev.InsanityWorldMod";
    }

    public static partial class G
    {
        public static GameManager       DredgeGame       => GameManager.Instance;
        public static Player            DredgePlayer     => GameManager.Instance?.Player;
        public static ApplicationEvents DredgeAppEvents  => ApplicationEvents.Instance;
        public static GameEvents        DredgeGameEvents => GameEvents.Instance;
    }

    /// <summary>
    /// First system to load (Order=0).
    /// Actions:
    /// - adds Core hooks
    /// - subscribes to DREDGE and Unity scene events
    /// - initializes Core.G.* state
    /// - spawns MainBehaviour
    /// - registers Harmony
    /// </summary>
    public class EntrySystem : IInsanityWorldSystem
    {
        public int Order => 0;

        public void OnLoad()
        {
            AddHooksLog();
            AddHooksWinch();
            AddHooksInput();
            AddHooksHud();
            AddHooksNotifications();
            AddHooksDocks();
            AddHooksPlayer();
            AddHooksItems();
            AddHooksSave();
            AddHooksDialogue();
            AddHooksFont();
            AddHooksText();
            AddHooksMenu();
            AddHooksPause();
            AddHooksSettings();

            Log.Info("EntrySystem.OnLoad: hooks added");

            AddListenersMenuScene();
            AddListenersGameScene();

            Log.Info("EntrySystem.OnLoad: listeners added");

            GameController.InitializeState();

            var main = new GameObject(nameof(InsanityWorldMod));
            main.AddComponent<MainBehaviour>();
            Object.DontDestroyOnLoad(main);

            new Harmony(HARMONY_ID).PatchAll(Assembly.GetExecutingAssembly());

            Log.Info("EntrySystem.OnLoad: done");
        }
    }
}
