using System;
using HarmonyLib;
using InsanityWorldMod.Core;
using static InsanityWorldMod.Core.DredgeHooks;
using static InsanityWorldMod.Core.Funcs;

namespace InsanityWorldMod.DredgeRuntime
{
    [HarmonyPatch(typeof(Player), nameof(Player.Die), new Type[0])]
    public static class PlayerDiePatcher
    {
        [HarmonyPrefix]
        public static bool Prefix(Player __instance)
        {
            if (__instance.IsGodModeEnabled || !__instance.IsAlive)
                return true;

            Log.Info("Death intercepted - repairing and returning to the last dock.");

            RepairHull(1);
            TeleportToLastDock();
            OnDeath();

            return false;
        }
    }
}
