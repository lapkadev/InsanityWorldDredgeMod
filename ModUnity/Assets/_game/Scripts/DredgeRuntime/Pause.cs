using InsanityWorldMod.Core;

namespace InsanityWorldMod.DredgeRuntime
{
    public static partial class Funcs
    {
        public static void AddHooksPause()
        {
            DredgeHooks.HideUnpausePrompt = () =>
            {
                var listener = G.DredgeGame?.PauseListener;
                if (listener == null)
                {
                    Log.Warn("Pause: pause listener is null");
                    return;
                }

                listener.CanShowUnpauseAction(false);
            };
        }
    }
}
