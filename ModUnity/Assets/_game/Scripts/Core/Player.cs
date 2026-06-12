using UnityEngine;

namespace InsanityWorldMod.Core
{
    public static partial class G
    {
        public static bool IsInGame => GameVanilla != null && GameVanilla.IsPlaying && Player != null;

        public static Transform PlayerTransform
        {
            get
            {
                var t = Player?.Controller?.transform;
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
