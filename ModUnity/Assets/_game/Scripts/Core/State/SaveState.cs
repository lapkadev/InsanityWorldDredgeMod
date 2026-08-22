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
        public const int CurrentSchemaVersion = 1;

        public int SchemaVersion = CurrentSchemaVersion;

        public int TotalRuns;
        public int TotalDeathsIntercepted;
        public int InsanityCellCharge;
    }
}
