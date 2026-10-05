using System.Linq;
using System.Reflection;
using HarmonyLib;
using Yarn.Unity;
using static InsW.DredgeRuntime.Constants;

namespace InsW.DredgeRuntime
{
    public static partial class Constants
    {
        public const string DIALOGUE_EXIT_TAG            = "exit";
        public const string DIALOGUE_EXIT_ACTION_FIELD   = "exitLineAction";
        public const string DIALOGUE_NEEDS_DISMISS_FIELD = "needsDismissing";
        public const string DIALOGUE_CONTINUE_METHOD     = "OnContinueLinePressComplete";
        public const string DIALOGUE_EXIT_PRESS_METHOD   = "OnExitLinePressComplete";
    }

    public static class DialogueQuickExit
    {
        private static DredgeDialogueView _armed;
        private static MethodInfo _continueMethod;

        public static void Arm(DredgeDialogueView view, LocalizedLine line)
        {
            Disarm();

            if (view == null || line.Metadata == null || !line.Metadata.Contains(DIALOGUE_EXIT_TAG))
                return;

            var action = GetExitAction(view);
            var input = G.DredgeGame?.Input;
            if (action == null || input == null)
                return;

            input.AddActionListener(new DredgePlayerActionBase[] { action }, ActionLayer.DIALOGUE);
            _armed = view;
        }

        public static void Disarm()
        {
            if (_armed == null)
                return;

            var action = GetExitAction(_armed);
            var input = G.DredgeGame?.Input;
            if (action != null && input != null)
                input.RemoveActionListener(new DredgePlayerActionBase[] { action }, ActionLayer.DIALOGUE);

            _armed = null;
        }

        public static bool TryHandlePress(DredgeDialogueView view)
        {
            if (_armed == null || _armed != view)
                return false;

            ContinueMethod().Invoke(view, null);

            if (!GetNeedsDismissing(view))
                Disarm();

            return true;
        }

        private static DredgePlayerActionPress GetExitAction(DredgeDialogueView view)
        {
            return AccessTools.FieldRefAccess<DredgeDialogueView, DredgePlayerActionPress>(view, DIALOGUE_EXIT_ACTION_FIELD);
        }

        private static bool GetNeedsDismissing(DredgeDialogueView view)
        {
            return AccessTools.FieldRefAccess<DredgeDialogueView, bool>(view, DIALOGUE_NEEDS_DISMISS_FIELD);
        }

        private static MethodInfo ContinueMethod()
        {
            if (_continueMethod == null)
                _continueMethod = AccessTools.Method(typeof(DredgeDialogueView), DIALOGUE_CONTINUE_METHOD);

            return _continueMethod;
        }
    }
}
