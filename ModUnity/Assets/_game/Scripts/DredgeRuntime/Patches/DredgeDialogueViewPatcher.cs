using System;
using HarmonyLib;
using InsW.Core;
using Yarn.Unity;
using static InsW.Core.Funcs;
using static InsW.DredgeRuntime.Constants;

namespace InsW.DredgeRuntime
{
    public static class DredgeDialogueViewPatcher
    {
        [HarmonyPatch(typeof(DredgeDialogueView), nameof(DredgeDialogueView.RunLine))]
        public static class RunLinePatch
        {
            [HarmonyPrefix]
            public static bool Prefix(LocalizedLine dialogueLine, Action onDialogueLineFinished)
            {
                if (DredgeEvents.ShouldDredgeRenderLine?.Invoke(dialogueLine.Metadata) != false)
                    return true;

                onDialogueLineFinished();
                return false;
            }

            [HarmonyPostfix]
            public static void Postfix(DredgeDialogueView __instance, LocalizedLine dialogueLine)
            {
                if (DredgeEvents.ShouldDredgeRenderLine?.Invoke(dialogueLine.Metadata) == false)
                    return;

                DialogueQuickExit.Arm(__instance, dialogueLine);
            }
        }

        [HarmonyPatch(typeof(DredgeDialogueView), nameof(DredgeDialogueView.RunOptions))]
        public static class RunOptionsPatch
        {
            [HarmonyPrefix]
            public static bool Prefix(DialogueOption[] dialogueOptions, Action<int> onOptionSelected)
            {
                DialogueQuickExit.Disarm();
                return DredgeEvents.ShouldDredgeRenderOptions?.Invoke() != false;
            }
        }

        [HarmonyPatch(typeof(DredgeDialogueView), DIALOGUE_EXIT_PRESS_METHOD)]
        public static class ExitLinePressPatch
        {
            [HarmonyPrefix]
            public static bool Prefix(DredgeDialogueView __instance)
            {
                return !DialogueQuickExit.TryHandlePress(__instance);
            }
        }

        [HarmonyPatch(typeof(DredgeDialogueView), nameof(DredgeDialogueView.Hide))]
        public static class HidePatch
        {
            [HarmonyPostfix]
            public static void Postfix()
            {
                DialogueQuickExit.Disarm();
            }
        }
    }
}
