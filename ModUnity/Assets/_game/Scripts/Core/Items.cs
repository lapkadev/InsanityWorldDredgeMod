namespace InsanityWorldMod.Core
{
    public static partial class Funcs
    {
        public static bool GiveItemToPlayer(string itemId)
        {
            if (string.IsNullOrEmpty(itemId))
            {
                Log.Warn("GiveItemToPlayer: empty item id");
                return false;
            }

            var given = DredgeHooks.GiveItemToPlayer(itemId);
            if (given)
                Log.Info($"GiveItemToPlayer: '{itemId}' added");
            else
                Log.Warn($"GiveItemToPlayer: '{itemId}' not added, no room or not ready");

            return given;
        }

        public static string[] GetEngineIds()
        {
            var ids = DredgeHooks.GetEngineIds();
            Log.Debug($"GetEngineIds: {ids.Length} found");
            return ids;
        }

        public static float GetEngineSpeed(string engineId)
        {
            if (string.IsNullOrEmpty(engineId))
            {
                Log.Warn("GetEngineSpeed: empty engine id");
                return 0f;
            }

            return DredgeHooks.GetEngineSpeed(engineId);
        }

        public static string GetItemShape(string itemId)
        {
            if (string.IsNullOrEmpty(itemId))
            {
                Log.Warn("GetItemShape: empty item id");
                return "";
            }

            return DredgeHooks.GetItemShape(itemId);
        }

        public static string GetItemName(string itemId)
        {
            if (string.IsNullOrEmpty(itemId))
            {
                Log.Warn("GetItemName: empty item id");
                return "";
            }

            return DredgeHooks.GetItemName(itemId);
        }
    }
}
