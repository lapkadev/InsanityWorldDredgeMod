using System.Linq;
using InsanityWorldMod.Core;
using InsanityWorldMod.Core.Dialogue;
using static InsanityWorldMod.Api.Constants;
using static InsanityWorldMod.Core.Constants;
using static InsanityWorldMod.Core.Funcs;

namespace InsanityWorldMod.Api
{
    public static partial class Constants
    {
        public const string YARN_FN_GET_CHARGE                     = "lapkadev_get_charge";
        public const string YARN_FN_GET_MAX_CHARGE                 = "lapkadev_get_max_charge";
        public const string YARN_FN_IS_CELL_FULL                   = "lapkadev_is_cell_full";
        public const string YARN_FN_GET_LAST_GRID_ABERRATION_COUNT = "lapkadev_get_last_grid_aberration_count";
        public const string YARN_FN_GET_LAST_GRID_ITEM_COUNT       = "lapkadev_get_last_grid_item_count";
        public const string YARN_FN_WAS_SUBMITTED                  = "lapkadev_was_submitted";

        public const string YARN_CMD_SET_EXPECTED                  = "lapkadev_set_expected";
        public const string YARN_CMD_CLEAR_EXPECTED                = "lapkadev_clear_expected";
        public const string YARN_CMD_SET_CONSUME_ABERRATIONS       = "lapkadev_set_consume_aberrations";
        public const string YARN_CMD_ADD_CHARGE                    = "lapkadev_add_charge";
        public const string YARN_CMD_ADD_CHARGE_PER_ABERRATION     = "lapkadev_add_charge_per_aberration";
    }

    public class DialogueSystem : IInsanityWorldSystem
    {
        public int Order => 10;

        public void OnLoad()
        {
            ApplicationEvents.Instance.OnGameLoaded += RegisterDialogueView;
            ApplicationEvents.Instance.OnGameLoaded += RegisterYarnCommands;
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

        private static void RegisterYarnCommands()
        {
            var runner = GameManager.Instance.DialogueRunner;
            if (runner == null) return;

            runner.AddFunction<int>(YARN_FN_GET_CHARGE, () => G.Save?.InsanityCellCharge ?? 0);
            runner.AddFunction<int>(YARN_FN_GET_MAX_CHARGE, () => INSANITY_CELL_MAX_CHARGE);
            runner.AddFunction<bool>(YARN_FN_IS_CELL_FULL, () => (G.Save?.InsanityCellCharge ?? 0) >= INSANITY_CELL_MAX_CHARGE);
            runner.AddFunction<int>(YARN_FN_GET_LAST_GRID_ABERRATION_COUNT, () => QuestGridPanelPatcher.LastGridAberrationCount);
            runner.AddFunction<string, int>(YARN_FN_GET_LAST_GRID_ITEM_COUNT, id =>
                QuestGridPanelPatcher.LastGridItemCounts.TryGetValue(id, out int c) ? c : 0);
            runner.AddFunction<bool>(YARN_FN_WAS_SUBMITTED, () => QuestGridPanelPatcher.WasSubmittedByDone);

            runner.AddCommandHandler<string, int>(YARN_CMD_SET_EXPECTED, (id, count) =>
            {
                QuestGridPanelPatcher.ExpectedItems[id] = count;
            });

            runner.AddCommandHandler(YARN_CMD_CLEAR_EXPECTED, () =>
            {
                QuestGridPanelPatcher.ExpectedItems.Clear();
                QuestGridPanelPatcher.ConsumeAberrations = false;
            });

            runner.AddCommandHandler(YARN_CMD_SET_CONSUME_ABERRATIONS, () =>
            {
                QuestGridPanelPatcher.ConsumeAberrations = true;
            });

            runner.AddCommandHandler<int>(YARN_CMD_ADD_CHARGE, count =>
            {
                if (G.Save == null) return;
                int newCharge = G.Save.InsanityCellCharge + count;
                if (newCharge > INSANITY_CELL_MAX_CHARGE) newCharge = INSANITY_CELL_MAX_CHARGE;
                G.Save.InsanityCellCharge = newCharge;
                G.Log.Info($"{YARN_CMD_ADD_CHARGE}: +{count} -> {newCharge}/{INSANITY_CELL_MAX_CHARGE}");
                Save();
            });

            runner.AddCommandHandler<int>(YARN_CMD_ADD_CHARGE_PER_ABERRATION, chargePerItem =>
            {
                if (G.Save == null) return;
                int count = QuestGridPanelPatcher.LastGridAberrationCount;
                int total = count * chargePerItem;
                int newCharge = G.Save.InsanityCellCharge + total;
                if (newCharge > INSANITY_CELL_MAX_CHARGE) newCharge = INSANITY_CELL_MAX_CHARGE;
                G.Save.InsanityCellCharge = newCharge;
                G.Log.Info($"{YARN_CMD_ADD_CHARGE_PER_ABERRATION}: {count} aberrations x {chargePerItem} = +{total} -> {newCharge}/{INSANITY_CELL_MAX_CHARGE}");
                Save();
            });

            G.Log.Info("RegisterYarnCommands: registered 6 functions + 5 commands");
        }
    }
}
