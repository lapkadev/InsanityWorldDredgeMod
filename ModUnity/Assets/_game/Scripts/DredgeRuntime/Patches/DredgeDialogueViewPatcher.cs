using System;
using HarmonyLib;
using InsanityWorldMod.Core;
using Yarn.Unity;
using static InsanityWorldMod.Core.Funcs;

namespace InsanityWorldMod.DredgeRuntime
{
    public static class DredgeDialogueViewPatcher
    {
        [HarmonyPatch(typeof(DredgeDialogueView), nameof(DredgeDialogueView.RunLine))]
        public static class RunLinePatch
        {
            [HarmonyPrefix]
            public static bool Prefix(LocalizedLine dialogueLine, Action onDialogueLineFinished)
            {
                if (ShouldVanillaRenderLine(dialogueLine.Metadata))
                    return true;

                onDialogueLineFinished();
                return false;
            }
        }

        [HarmonyPatch(typeof(DredgeDialogueView), nameof(DredgeDialogueView.RunOptions))]
        public static class RunOptionsPatch
        {
            [HarmonyPrefix]
            public static bool Prefix(DialogueOption[] dialogueOptions, Action<int> onOptionSelected)
            {
                return ShouldVanillaRenderOptions();
            }
        }
    }
}
