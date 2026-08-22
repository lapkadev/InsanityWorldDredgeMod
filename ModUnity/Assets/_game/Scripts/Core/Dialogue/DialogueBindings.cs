using System.Linq;
using InsanityWorldMod.Core.Dialogue;
using static InsanityWorldMod.Core.Constants;
using static InsanityWorldMod.Core.DredgeHooks;

namespace InsanityWorldMod.Core
{
    public static partial class Constants
    {
        public const string YARN_CMD_LOG                = "lapkadev_log";
        public const bool   USE_VANILLA_DIALOGUE_ALWAYS = true;
    }

    public static partial class G
    {
        public static InsanityDialogueView DialogueView;
    }

    public static partial class Funcs
    {
        public static void LogFromYarn(string message)
        {
            Log.Info($"[SMOKE-yarn] {message}");
        }

        public static string GetCurrentDialogueNode()
        {
            return GetDialogueRunner()?.CurrentNodeName ?? "";
        }

        public static void RegisterDialogueView()
        {
            if (G.DialogueView != null)
                return;

            var runner = GetDialogueRunner();
            if (runner == null)
            {
                Log.Warn("RegisterDialogueView: dialogue runner is null, skipping");
                return;
            }

            G.DialogueView = InsanityDialogueView.Spawn();
            runner.dialogueViews = runner.dialogueViews.Append(G.DialogueView).ToArray();

            Log.Info($"RegisterDialogueView: attached InsanityDialogueView (total views: {runner.dialogueViews.Length})");
        }

        public static void RegisterYarnBindings()
        {
            var runner = GetDialogueRunner();
            if (runner == null)
            {
                Log.Warn("RegisterYarnBindings: dialogue runner is null, skipping");
                return;
            }

            runner.AddFunction<int>(YARN_FN_GET_CHARGE, GetInsanityCharge);
            runner.AddFunction<int>(YARN_FN_GET_MAX_CHARGE, GetInsanityMaxCharge);
            runner.AddFunction<bool>(YARN_FN_IS_CELL_FULL, IsInsanityCellFull);
            runner.AddFunction<int>(YARN_FN_GET_LAST_GRID_ABERRATION_COUNT, GetLastGridAberrationCount);
            runner.AddFunction<string, int>(YARN_FN_GET_LAST_GRID_ITEM_COUNT, GetLastGridItemCount);
            runner.AddFunction<bool>(YARN_FN_WAS_SUBMITTED, WasQuestGridSubmitted);

            runner.AddCommandHandler<string, int>(YARN_CMD_SET_EXPECTED, SetExpectedItem);
            runner.AddCommandHandler(YARN_CMD_CLEAR_EXPECTED, ClearExpectedItems);
            runner.AddCommandHandler(YARN_CMD_SET_CONSUME_ABERRATIONS, SetConsumeAberrations);
            runner.AddCommandHandler<int>(YARN_CMD_ADD_CHARGE, AddInsanityCharge);
            runner.AddCommandHandler<int>(YARN_CMD_ADD_CHARGE_PER_ABERRATION, AddInsanityChargePerAberration);
            runner.AddCommandHandler<string>(YARN_CMD_LOG, LogFromYarn);

            Log.Info("RegisterYarnBindings: registered 6 functions + 6 commands");
        }

        public static bool ShouldVanillaRenderLine(string[] tags)
        {
            if (USE_VANILLA_DIALOGUE_ALWAYS)
                return true;

            bool isOurNode = GetCurrentDialogueNode().StartsWith(PREFIX);
            bool hasVanillaTag = HasDialogueTag(tags, TAG_VANILLA_UI);
            bool hasLapkadevTag = HasDialogueTag(tags, TAG_LAPKADEV_UI);

            return (!isOurNode && !hasLapkadevTag) || (isOurNode && hasVanillaTag);
        }

        public static bool ShouldVanillaRenderOptions()
        {
            if (USE_VANILLA_DIALOGUE_ALWAYS)
                return true;

            return !GetCurrentDialogueNode().StartsWith(PREFIX);
        }

        public static bool HasDialogueTag(string[] tags, string tag)
        {
            if (tags == null)
                return false;

            foreach (var entry in tags)
            {
                if (entry == tag)
                    return true;
            }

            return false;
        }
    }
}
