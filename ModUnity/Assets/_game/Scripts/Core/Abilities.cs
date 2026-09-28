namespace InsanityWorldMod.Core
{
    public static partial class Funcs
    {
        public static string[] GetAbilityIds()
        {
            var ids = DredgeHooks.GetAbilityIds();
            Log.Debug($"GetAbilityIds: {ids.Length} found");
            return ids;
        }

        public static bool IsAbilityUnlocked(string abilityId)
        {
            if (string.IsNullOrEmpty(abilityId))
                return false;

            return DredgeHooks.IsAbilityUnlocked(abilityId);
        }

        public static void SetAbilityUnlocked(string abilityId, bool unlocked)
        {
            if (string.IsNullOrEmpty(abilityId))
            {
                Log.Warn("SetAbilityUnlocked: empty ability id");
                return;
            }

            DredgeHooks.SetAbilityUnlocked(abilityId, unlocked);
            Log.Info($"SetAbilityUnlocked: '{abilityId}' unlocked = {unlocked}");
        }
    }
}
