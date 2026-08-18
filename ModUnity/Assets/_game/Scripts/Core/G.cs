using System;
using InsanityWorldMod.Core.Dialogue;

namespace InsanityWorldMod.Core
{
    public static partial class G
    {
        // Our state
        public static GameState            Game         { get; set; }
        public static RunState             Run          { get; set; }
        public static SaveState            Save         { get; set; }
        public static InsanityDialogueView DialogueView { get; set; }
        public static string               ModBasePath  { get; set; }

    }
}
