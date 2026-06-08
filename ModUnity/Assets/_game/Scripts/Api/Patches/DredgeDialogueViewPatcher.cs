using System;
using System.Linq;
using HarmonyLib;
using InsanityWorldMod.Core;
using Yarn.Unity;
using static InsanityWorldMod.Core.Constants;

namespace InsanityWorldMod.Api
{
    public static class DredgeDialogueViewPatcher
    {
        [HarmonyPatch(typeof(DredgeDialogueView), nameof(DredgeDialogueView.RunLine))]
        public static class RunLinePatch
        {
            [HarmonyPrefix]
            public static bool Prefix(LocalizedLine dialogueLine, Action onDialogueLineFinished)
            {
                if (USE_VANILLA_DIALOGUE_ALWAYS) return true;
                if (VanillaShouldRender(dialogueLine)) return true;
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
                if (USE_VANILLA_DIALOGUE_ALWAYS) return true;
                var currentNode = GameManager.Instance?.DialogueRunner?.CurrentNodeName ?? "";
                if (!currentNode.StartsWith(PREFIX)) return true;
                return false;
            }
        }

        private static bool VanillaShouldRender(LocalizedLine line)
        {
            var currentNode = GameManager.Instance?.DialogueRunner?.CurrentNodeName ?? "";
            bool isOurNode = currentNode.StartsWith(PREFIX);
            bool hasVanillaTag = HasTag(line, TAG_VANILLA_UI);
            bool hasLapkadevTag = HasTag(line, TAG_LAPKADEV_UI);
            return (!isOurNode && !hasLapkadevTag) || (isOurNode && hasVanillaTag);
        }

        private static bool HasTag(LocalizedLine line, string tag)
        {
            return line.Metadata?.Any(m => m == tag) == true;
        }
    }
}
