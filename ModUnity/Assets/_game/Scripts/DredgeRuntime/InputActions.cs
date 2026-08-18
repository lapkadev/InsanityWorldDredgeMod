using System;
using System.Collections.Generic;
using InControl;
using InsanityWorldMod.Core;
using static InsanityWorldMod.DredgeRuntime.Constants;

namespace InsanityWorldMod.DredgeRuntime
{
    public static partial class Constants
    {
        public const string BACK_ACTION_PROMPT_KEY = "prompt.back";
    }

    public static partial class G
    {
        public static Dictionary<int, DredgePlayerActionPress> BackActions = new Dictionary<int, DredgePlayerActionPress>();

        public static int LastBackActionHandle;
    }

    public static partial class Funcs
    {
        public static void AddHooksInput()
        {
            DredgeHooks.AddInputBackAction = (onBack, showPrompt) =>
            {
                var input = G.DredgeGame?.Input;
                if (input == null)
                {
                    Log.Warn("Input: input manager is null, back action not created");
                    return 0;
                }

                var action = new DredgePlayerActionPress(BACK_ACTION_PROMPT_KEY, input.Controls.Back);
                action.showInControlArea = showPrompt;
                action.evaluateWhenPaused = true;
                action.OnPressComplete = onBack;
                input.AddActionListener(new DredgePlayerActionBase[] { action }, ActionLayer.SYSTEM);

                G.LastBackActionHandle++;
                G.BackActions[G.LastBackActionHandle] = action;
                return G.LastBackActionHandle;
            };

            DredgeHooks.RemoveInputBackAction = handle =>
            {
                if (!G.BackActions.TryGetValue(handle, out var action))
                    return;

                G.DredgeGame?.Input?.RemoveActionListener(new DredgePlayerActionBase[] { action }, ActionLayer.SYSTEM);
                G.BackActions.Remove(handle);
            };

            DredgeHooks.GetActionIcon = (action, pressed) =>
            {
                var icon = G.DredgeGame?.Input?.GetControlIconForActionWithDefault(action);
                if (icon == null)
                    return null;

                return pressed ? icon.downSprite : icon.upSprite;
            };

            DredgeHooks.SubscribeInputChanged = handler =>
            {
                var input = G.DredgeGame?.Input;
                if (input == null)
                    return;

                input.OnInputChanged = (Action<BindingSourceType, InputDeviceStyle>)Delegate.Combine(input.OnInputChanged, handler);
            };

            DredgeHooks.UnsubscribeInputChanged = handler =>
            {
                var input = G.DredgeGame?.Input;
                if (input == null)
                    return;

                input.OnInputChanged = (Action<BindingSourceType, InputDeviceStyle>)Delegate.Remove(input.OnInputChanged, handler);
            };
        }
    }
}
