using System;
using InsanityWorldMod.Core.Dialogue;

namespace InsanityWorldMod.Core
{
    public static partial class G
    {
        // Our state
        public static GameState            Game;
        public static RunState             Run;
        public static SaveState            Save;
        public static InsanityDialogueView DialogueView;
        public static string               ModBasePath;
    }
}
