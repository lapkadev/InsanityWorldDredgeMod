namespace InsanityWorldMod.Core
{
    public static partial class Constants
    {
        public const string GREATER_MARROW_DOCK_ID = "dock.greater-marrow";
        public const string MYSTIC_SPEAKER_ID      = "lapkadev_mystic";

        public static readonly SpeakerInjection[] SPEAKER_INJECTIONS =
        {
            new SpeakerInjection { DockId = GREATER_MARROW_DOCK_ID, SpeakerId = MYSTIC_SPEAKER_ID },
        };
    }

    public struct SpeakerInjection
    {
        public string DockId;
        public string SpeakerId;
    }
}
