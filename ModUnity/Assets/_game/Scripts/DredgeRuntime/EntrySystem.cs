using InsW.Core;
using UnityEngine;
using static InsW.DredgeRuntime.Constants;
using static InsW.DredgeRuntime.Funcs;

namespace InsW.DredgeRuntime
{
    /// <summary>
    /// First system to load (Order=0).
    /// Actions:
    /// - adds Core hooks
    /// - subscribes to DREDGE and Unity scene events
    /// - adds Harmony patches, each one stays off until enabled through DredgePatches
    /// </summary>
    public class EntrySystem : IModSystem
    {
        public int Order => 0;

        public void OnLoad()
        {
            AddHooksLog();
            AddHooksWinch();
            AddHooksInput();
            AddHooksHud();
            AddHooksThreats();
            AddHooksNotifications();
            AddHooksTeleport();
            AddHooksDocks();
            AddHooksPlayer();
            AddHooksItems();
            AddHooksSave();
            AddHooksDialogue();
            AddHooksNpc();
            AddHooksNpcCamera();
            AddHooksFont();
            AddHooksText();
            AddHooksMenu();
            AddHooksPause();
            AddHooksSettings();
            AddHooksWorld();
            AddHooksAbilities();
            AddHooksQuestGrid();

            Log.Info("EntrySystem.OnLoad: hooks added");

            AddEventsMenu();
            AddEventsGameScene();

            Log.Info("EntrySystem.OnLoad: events added");

            AddPatches();

            Log.Info("EntrySystem.OnLoad: done");
        }
    }
}
