namespace InsanityWorldMod.Core
{
    public static partial class Funcs
    {
        public static string[] GetAberrationFishIds()
        {
            var ids = DredgeHooks.GetAberrationFishIds();
            Log.Debug($"GetAberrationFishIds: {ids.Length} found");
            return ids;
        }

        public static bool GiveFishToPlayer(string fishId)
        {
            if (string.IsNullOrEmpty(fishId))
            {
                Log.Warn("GiveFishToPlayer: empty fish id");
                return false;
            }

            var given = DredgeHooks.GiveFishToPlayer(fishId);
            if (given)
                Log.Info($"GiveFishToPlayer: '{fishId}' added");
            else
                Log.Warn($"GiveFishToPlayer: '{fishId}' not added, no room or not ready");

            return given;
        }
    }
}
