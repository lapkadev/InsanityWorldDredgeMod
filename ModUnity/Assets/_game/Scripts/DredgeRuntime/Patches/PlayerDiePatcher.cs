using System;
using HarmonyLib;
using InsW.Core;
using static InsW.Core.DredgeHooks;
using static InsW.Core.Funcs;

namespace InsW.DredgeRuntime
{
    [HarmonyPatch(typeof(Player), nameof(Player.Die), new Type[0])]
    public static class PlayerDiePatcher
    {
        private static bool _isHandlingDeath;

        [HarmonyPrefix]
        public static bool Prefix(Player __instance)
        {
            if (__instance.IsGodModeEnabled || !__instance.IsAlive)
                return true;

            if (_isHandlingDeath || G.Teleport.IsRunning)
            {
                Log.Info("Death repeated while the previous one is handled - repairing only.");
                RepairHull(1);
                return false;
            }

            Log.Info("Death intercepted - repairing and returning to the last dock.");

            _isHandlingDeath = true;
            try
            {
                RepairHull(1);
                DredgeEvents.OnPlayerDied?.Invoke();
            }
            finally
            {
                _isHandlingDeath = false;
            }

            return false;
        }
    }
}
