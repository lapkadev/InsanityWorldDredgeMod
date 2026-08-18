using InsanityWorldMod.Core;

namespace InsanityWorldMod.DredgeRuntime
{
    public static partial class Funcs
    {
        public static void AddHooksItems()
        {
            DredgeHooks.RepairHull = () =>
            {
                var items = G.DredgeGame?.ItemManager;
                if (items == null)
                {
                    Log.Warn("Items: item manager is null");
                    return;
                }

                items.RepairHullDamage(free: true);
            };

            DredgeHooks.RepairAllItems = () =>
            {
                var items = G.DredgeGame?.ItemManager;
                if (items == null)
                {
                    Log.Warn("Items: item manager is null");
                    return;
                }

                items.RepairAllItemDurability();
            };
        }
    }
}
