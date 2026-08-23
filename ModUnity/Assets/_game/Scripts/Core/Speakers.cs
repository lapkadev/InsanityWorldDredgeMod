namespace InsanityWorldMod.Core
{
    public static partial class Constants
    {
        public const string GREATER_MARROW_DOCK_ID = "dock.greater-marrow";
        public const string MYSTIC_SPEAKER_ID      = "insanity_mystic";
        public const string MYSTIC_ROOT_NODE       = "insanity_mystic_root";
        public const string PFB_NPC_MYSTIC         = "pfb_npc_mystic";
        public const string PFB_NPC_MYSTIC_CAMERA  = "pfb_npc_mystic_camera";
        public const string TEX_MYSTIC_HEAD        = "tex_mystic_head";

        public static readonly SpeakerInjection[] SPEAKER_INJECTIONS =
        {
            new SpeakerInjection
            {
                DockId     = GREATER_MARROW_DOCK_ID,
                SpeakerId  = MYSTIC_SPEAKER_ID,
                PrefabName       = PFB_NPC_MYSTIC,
                CameraPrefabName = PFB_NPC_MYSTIC_CAMERA,
                IconName         = TEX_MYSTIC_HEAD,
            },
        };
    }

    public struct SpeakerInjection
    {
        public string DockId;
        public string SpeakerId;
        public string PrefabName;
        public string CameraPrefabName;
        public string IconName;
    }
}
