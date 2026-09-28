using HarmonyLib;
using InsanityWorldMod.Core;
using static InsanityWorldMod.Core.Funcs;
using static InsanityWorldMod.DredgeRuntime.Constants;

namespace InsanityWorldMod.DredgeRuntime
{
    public static partial class Constants
    {
        public const string LEVIATHAN_KILL_METHOD               = "KillPlayer";
        public const string LEVIATHAN_DISABLE_MOVEMENT_METHOD   = "DisableMovement";
        public const string LEVIATHAN_DISABLE_BOAT_MODEL_METHOD = "DisableBoatModel";
    }

    [HarmonyPatch(typeof(LeviathanAnimationEvents), LEVIATHAN_DISABLE_MOVEMENT_METHOD)]
    public static class LeviathanDisableMovementPatcher
    {
        [HarmonyPrefix]
        public static bool Prefix()
        {
            Log.Info("Leviathan movement lock skipped.");
            return false;
        }
    }

    [HarmonyPatch(typeof(LeviathanAnimationEvents), LEVIATHAN_DISABLE_BOAT_MODEL_METHOD)]
    public static class LeviathanDisableBoatModelPatcher
    {
        [HarmonyPrefix]
        public static bool Prefix(LeviathanAnimationEvents __instance)
        {
            var game = G.DredgeGame;
            var player = G.DredgePlayer;
            if (game == null || player == null)
                return false;

            if (__instance.HasPlayerTeleportedAway || player.IsGodModeEnabled)
                return false;

            if (game.WorldEventManager != null && game.WorldEventManager.DoesHitSafeZone(player.transform.position))
                return false;

            Log.Info("Leviathan bite intercepted - hull damage, boat model stays visible.");
            OnLeviathanStrike();
            return false;
        }
    }

    [HarmonyPatch(typeof(LeviathanAnimationEvents), LEVIATHAN_KILL_METHOD)]
    public static class LeviathanKillPatcher
    {
        [HarmonyPrefix]
        public static bool Prefix(LeviathanAnimationEvents __instance)
        {
            Log.Info("Leviathan kill skipped - event finished.");

            var worldEvent = __instance.GetComponentInParent<LeviathanWorldEvent>();
            if (worldEvent != null)
                worldEvent.RequestEventFinish();

            return false;
        }
    }
}
