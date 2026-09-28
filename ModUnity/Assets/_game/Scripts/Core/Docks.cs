namespace InsanityWorldMod.Core
{
    public static partial class Funcs
    {
        public static string[] GetDockIds()
        {
            var ids = DredgeHooks.GetDockIds();
            Log.Debug($"GetDockIds: {ids.Length} found");
            return ids;
        }

        public static string GetDockName(string dockId)
        {
            if (string.IsNullOrEmpty(dockId))
            {
                Log.Warn("GetDockName: empty dock id");
                return "";
            }

            return DredgeHooks.GetDockName(dockId);
        }
    }
}
