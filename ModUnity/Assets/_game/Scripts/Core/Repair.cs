using static InsanityWorldMod.Core.DredgeHooks;

namespace InsanityWorldMod.Core
{
    public static partial class Funcs
    {
        public static void RepairFull()
        {
            RepairHull();
            RepairAllItems();
            Log.Debug("RepairFull: hull + all items repaired");
        }
    }
}
