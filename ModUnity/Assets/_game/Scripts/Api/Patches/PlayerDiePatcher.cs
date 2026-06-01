using System;
using HarmonyLib;
using InsanityWorldMod.Core;
using static InsanityWorldMod.Core.Funcs;

namespace InsanityWorldMod.Api
{
    /// <summary>
    // Player.Die has two overloads (parameterless + debug command Die(CommandArg[])).
    // Target the parameterless one - the debug command internally calls it anyway.
    /// </summary>
    [HarmonyPatch(typeof(Player), nameof(Player.Die), new Type[0])]
    public static class PlayerDiePatcher
    {
        [HarmonyPrefix]
        public static bool Prefix(Player __instance)
        {
            if (__instance.IsGodModeEnabled || !__instance.IsAlive)
                return true;

            G.Log.Info("Death intercepted - restarting run.");

            RepairFull();
            TeleportToLastDock();
            OnDeathIntercepted();

            return false;
        }
    }
}
