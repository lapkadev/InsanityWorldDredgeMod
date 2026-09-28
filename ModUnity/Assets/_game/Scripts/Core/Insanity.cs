using UnityEngine;
using static InsanityWorldMod.Core.Constants;
using static InsanityWorldMod.Core.DredgeHooks;

namespace InsanityWorldMod.Core
{
    public static partial class Constants
    {
        public const float INSANITY_BASE_MAX = 1f;
        public const float INSANITY_MAX      = 100f;
    }

    public static partial class G
    {
        public static bool  InsanitySynced;
        public static float LastSanity;
    }

    public static partial class Funcs
    {
        public static void PushInsanity()
        {
            ChangeSanity(INSANITY_BASE_MAX - G.Game.InsanityLevel - GetSanity());
            G.LastSanity = GetSanity();
            G.InsanitySynced = true;
        }

        public static void ResetInsanitySync()
        {
            G.InsanitySynced = false;
        }

        public static void TickInsanity()
        {
            if (G.Game == null || !G.Game.IsLoaded || !G.IsInGame)
                return;

            if (!G.InsanitySynced)
            {
                PushInsanity();
                return;
            }

            float sanity = GetSanity();
            float sanityDelta = sanity - G.LastSanity;
            if (sanity <= 0f)
                sanityDelta = Mathf.Min(sanityDelta, GetSanityFrameDelta());

            G.Game.InsanityLevel -= sanityDelta;
            PushInsanity();
        }
    }
}
