using static InsanityWorldMod.Core.Constants;
using static InsanityWorldMod.Core.DredgeHooks;

namespace InsanityWorldMod.Core
{
    public static partial class Constants
    {
        public const int LEVIATHAN_STRIKE_DAMAGE = 4;
    }

    public static partial class Funcs
    {
        public static void OnLeviathanStrike()
        {
            DamageHull(LEVIATHAN_STRIKE_DAMAGE);
            Log.Info($"OnLeviathanStrike: {LEVIATHAN_STRIKE_DAMAGE} hull damage applied");
        }
    }
}
