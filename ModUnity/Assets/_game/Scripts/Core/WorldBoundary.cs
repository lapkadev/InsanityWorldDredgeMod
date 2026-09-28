using static InsanityWorldMod.Core.DredgeHooks;

namespace InsanityWorldMod.Core
{
    public static partial class Funcs
    {
        public static bool StartWorldEvent(string eventId)
        {
            if (string.IsNullOrEmpty(eventId))
            {
                Log.Warn("StartWorldEvent: empty event id");
                return false;
            }

            bool started = DredgeHooks.StartWorldEvent(eventId);
            if (started)
                Log.Info($"StartWorldEvent: '{eventId}' started");
            else
                Log.Warn($"StartWorldEvent: '{eventId}' not started");

            return started;
        }

        public static void RefreshBoundaryGuard()
        {
            bool guarded = !HasCompass();
            SetBoundaryGuardEnabled(guarded);
            Log.Info($"RefreshBoundaryGuard: world boundary guarded = {guarded}");
        }
    }
}
