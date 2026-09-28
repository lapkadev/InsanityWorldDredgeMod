using System.Reflection;
using HarmonyLib;
using InsanityWorldMod.Core;
using UnityEngine;
using static InsanityWorldMod.Core.Funcs;
using static InsanityWorldMod.DredgeRuntime.Constants;

namespace InsanityWorldMod.DredgeRuntime
{
    public static partial class Constants
    {
        public const string WINCH_MODS_TAB_TYPE        = "Winch.Components.ModsTab";
        public const string WINCH_MODS_TAB_POPULATE    = "PopulateList";
        public const string WINCH_MODS_TAB_INSTANCE    = "Instance";
        public const string WINCH_MODS_TAB_LIST_FIELD  = "list";
        public const string WINCH_MODS_TAB_PANEL_FIELD = "panel";
        public const string WINCH_MOD_LABEL_SUFFIX     = " Label";
        public const string WINCH_MOD_BUTTON_SUFFIX    = " Button";
    }

    [HarmonyPatch]
    public static class WinchModsTabPatcher
    {
        public static bool Prepare()
        {
            if (TargetMethod() != null)
                return true;

            // Log.Warn($"WinchModsTabPatcher: {WINCH_MODS_TAB_TYPE}.{WINCH_MODS_TAB_POPULATE} not found, mods list rebuild is not reported");
            return false;
        }

        public static MethodBase TargetMethod()
        {
            var type = AccessTools.TypeByName(WINCH_MODS_TAB_TYPE);
            return type == null ? null : AccessTools.Method(type, WINCH_MODS_TAB_POPULATE);
        }

        [HarmonyPostfix]
        public static void Postfix(object __instance)
        {
            var panel = Traverse.Create(__instance).Field(WINCH_MODS_TAB_PANEL_FIELD).GetValue<Component>();
            OnModsListShown(panel != null ? panel.transform as RectTransform : null);
        }
    }
}
