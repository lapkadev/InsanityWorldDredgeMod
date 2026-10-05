using HarmonyLib;
using InsW.Core;
using UnityEngine;
using static InsW.DredgeRuntime.Constants;

namespace InsW.DredgeRuntime
{
    public static partial class Constants
    {
        public const string QUEST_GRID_HELP_FIELD = "helpTextContainer";
    }

    public static partial class Funcs
    {
        public static void AddHooksQuestGrid()
        {
            DredgeHooks.SetQuestGridHelpVisible = isVisible =>
            {
                var panel = G.DredgeGame?.UI?.QuestGridPanel;
                if (panel == null)
                {
                    Log.Warn("QuestGrid: quest grid panel is null");
                    return;
                }

                var help = AccessTools.Field(typeof(QuestGridPanel), QUEST_GRID_HELP_FIELD).GetValue(panel) as GameObject;
                if (help == null)
                {
                    Log.Warn("QuestGrid: help text container is null");
                    return;
                }

                help.SetActive(isVisible);
            };
        }
    }
}
