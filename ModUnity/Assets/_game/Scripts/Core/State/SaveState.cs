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

        public bool  IslandPlaced;
        public float IslandX;
        public float IslandZ;
        public float IslandHeadingDeg;
        public float IslandJumpTimeLeft;

        public float? InsanityLevel;

        private Statistics _stats;
        public Statistics Stats
        {
            get => _stats ??= new Statistics();
            set => _stats = value ?? new Statistics();
        }
    }
}
