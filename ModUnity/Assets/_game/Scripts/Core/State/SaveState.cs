using System;

namespace InsanityWorldMod.Core
{
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
