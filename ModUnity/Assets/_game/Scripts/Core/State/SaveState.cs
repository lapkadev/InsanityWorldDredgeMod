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

        public int TotalRuns;
        public int TotalDeathsIntercepted;
        public int InsanityCellCharge;
    }
}
