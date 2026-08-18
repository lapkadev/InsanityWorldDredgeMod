using UnityEngine;
using static InsanityWorldMod.Core.DredgeHooks;

namespace InsanityWorldMod.Core
{
    public static partial class G
    {
        public static bool IsInGame => DredgeHooks.IsInGame();

        public static Transform PlayerTransform
        {
            get
            {
                var t = GetPlayerTransform();
                if (t == null)
                {
                    Log.Warn("PlayerTransform: null in chain (no game / not loaded), returning fallback");
                    return Fallbacks.Transform;
                }
                return t;
            }
        }
    }
}
