namespace InsanityWorldMod.Core
{
    /// <summary>
    /// Core-side constants (gameplay tuning, paths, defaults).
    /// Partial - can be split across multiple files within Core.
    /// </summary>
    public static partial class Constants
    {
        public const string PREFIX                         = "lapkadev_";
        public const bool   USE_DEBUG_PATH                 = false;
        public const float  AUTO_SAVE_INTERVAL_SEC         = 60f;
        public const string DEFAULT_RESTART_DOCK           = "dock.greater-marrow";
        public const bool   USE_VANILLA_DIALOGUE_ALWAYS    = true;
        public const int    INSANITY_CELL_MAX_CHARGE       = 100;
        public const int    INSANITY_CHARGE_PER_ABERRATION = 10;
    }
}
