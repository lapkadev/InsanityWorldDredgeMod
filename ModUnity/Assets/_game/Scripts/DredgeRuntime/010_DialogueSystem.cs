using InsanityWorldMod.Core;
using static InsanityWorldMod.Core.Funcs;

namespace InsanityWorldMod.DredgeRuntime
{
    public class DialogueSystem : IInsanityWorldSystem
    {
        public int Order => 10;

        public void OnLoad()
        {
            G.DredgeAppEvents.OnGameLoaded += () =>
            {
                RegisterDialogueView();
                RegisterYarnBindings();
            };

            Log.Info("DialogueSystem.OnLoad: subscribed to OnGameLoaded");
        }
    }
}
