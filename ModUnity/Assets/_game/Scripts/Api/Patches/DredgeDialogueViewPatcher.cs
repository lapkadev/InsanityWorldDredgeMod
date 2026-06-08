using System;
using System.Linq;
using HarmonyLib;
using Yarn.Unity;

namespace InsanityWorldMod.Api
{
    public static class DredgeDialogueViewPatcher
    {
        private const string OUR_NODE_PREFIX = "lapkadev_";
        private const string TAG_VANILLA_UI = "vanilla_ui";
        private const string TAG_LAPKADEV_UI = "lapkadev_ui";

        [HarmonyPatch(typeof(DredgeDialogueView), nameof(DredgeDialogueView.RunLine))]
        public static class RunLinePatch
        {
            [HarmonyPrefix]
            public static bool Prefix(LocalizedLine dialogueLine, Action onDialogueLineFinished)
            {
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
                var currentNode = GameManager.Instance?.DialogueRunner?.CurrentNodeName ?? "";
                if (!currentNode.StartsWith(OUR_NODE_PREFIX)) return true;
                return false;
            }
        }

        private static bool VanillaShouldRender(LocalizedLine line)
        {
            var currentNode = GameManager.Instance?.DialogueRunner?.CurrentNodeName ?? "";
            bool isOurNode = currentNode.StartsWith(OUR_NODE_PREFIX);
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
