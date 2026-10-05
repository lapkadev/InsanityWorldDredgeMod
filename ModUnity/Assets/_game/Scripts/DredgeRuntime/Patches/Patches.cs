using System;
using System.Collections.Generic;
using HarmonyLib;
using InsW.Core;
using static InsW.DredgeRuntime.Constants;

namespace InsW.DredgeRuntime
{
    public static partial class Constants
    {
        public const string HARMONY_ID = "lapkadev.InsanityWorldMod";
    }

    public static partial class G
    {
        public static Harmony Harmony;
        public static HashSet<Type> EnabledPatchers = new HashSet<Type>();
    }

    public static partial class Funcs
    {
        public static void AddPatches()
        {
            G.Harmony = new Harmony(HARMONY_ID);

            DredgePatches.CancelPlayerDeath = () => EnablePatcher(typeof(PlayerDiePatcher));
            DredgePatches.CancelDeathByLeviathan = () =>
            {
                EnablePatcher(typeof(LeviathanDisableMovementPatcher));
                EnablePatcher(typeof(LeviathanDisableBoatModelPatcher));
                EnablePatcher(typeof(LeviathanKillPatcher));
            };
            DredgePatches.ReturnQuestGridItemsOnClose = () => EnablePatcher(typeof(QuestGridPanelPatcher));
            DredgePatches.ExitFromDialogueLineWithTag = () => EnablePatcher(typeof(DredgeDialogueViewPatcher));

            WinchPatches.NotifyModsListRebuilt = () => EnablePatcher(typeof(WinchModsTabPatcher));
        }

        public static void EnablePatcher(Type patcher)
        {
            if (!G.EnabledPatchers.Add(patcher))
                return;

            G.Harmony.CreateClassProcessor(patcher).Patch();
            foreach (var nested in patcher.GetNestedTypes())
                G.Harmony.CreateClassProcessor(nested).Patch();

            DevLog.Info($"EnablePatcher: {patcher.Name} enabled");
        }
    }
}
