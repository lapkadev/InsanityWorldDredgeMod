using InsanityWorldMod.Core;

namespace InsanityWorldMod.DredgeRuntime
{
    public static partial class Funcs
    {
        public static void AddHooksDialogue()
        {
            DredgeHooks.GetDialogueRunner = () => G.DredgeGame?.DialogueRunner;
        }
    }
}
