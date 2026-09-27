using System;

namespace InsanityWorldMod.Core
{
    public static partial class G
    {
        public static SaveState Save;
    }

    [Serializable]
    public class SaveState
    {
        public const int CURRENT_VERSION = 2;

        public int Version = CURRENT_VERSION;

        public bool CompassGranted;
        public bool CompassIsNew;

        private Statistics _stats;
        public Statistics Stats
        {
            get => _stats ??= new Statistics();
            set => _stats = value ?? new Statistics();
        }
    }
}
