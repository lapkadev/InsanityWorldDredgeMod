using System.Linq;
using InsanityWorldMod.Core;
using InsanityWorldMod.Core.Dialogue;

namespace InsanityWorldMod.Api
{
    public class DialogueSystem : IInsanityWorldSystem
    {
        public int Order => 10;

        public void OnLoad()
        {
            ApplicationEvents.Instance.OnGameLoaded += RegisterDialogueView;
            G.Log.Info("DialogueSystem.OnLoad: subscribed to OnGameLoaded");
        }

        private static void RegisterDialogueView()
        {
            if (G.DialogueView != null) return;

            var runner = GameManager.Instance.DialogueRunner;
            if (runner == null)
            {
                G.Log.Warn("RegisterDialogueView: DialogueRunner is null, skipping");
                return;
            }

            G.DialogueView = InsanityDialogueView.Spawn();
            runner.dialogueViews = runner.dialogueViews.Append(G.DialogueView).ToArray();

            G.Log.Info($"RegisterDialogueView: attached InsanityDialogueView (total views: {runner.dialogueViews.Length})");
        }
    }
}
