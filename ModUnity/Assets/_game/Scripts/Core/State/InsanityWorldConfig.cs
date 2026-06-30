using System;

namespace InsanityWorldMod.Core
{
    [Serializable]
    public class InsanityWorldConfig
    {
        public const int CurrentVersion = 1;

        public int Version = CurrentVersion;

        public bool IsTransitionPhaseCompleted;

        public bool IsDev;
    }
}
