using System;
using System.Linq;
using static InsanityWorldMod.Core.Constants;
using static InsanityWorldMod.Core.DredgeHooks;

namespace InsanityWorldMod.Core
{
    public static partial class Constants
    {
        public const string YARN_CMD_LOG                = "insanity_log";
        public const bool   USE_DREDGE_DIALOGUE_ALWAYS = true;
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

        public static bool HasVisitedNode(string node)
        {
            if (string.IsNullOrEmpty(node))
            {
                Log.Warn("HasVisitedNode: node name is empty");
                return false;
            }

            return IsDialogueNodeVisited(node);
        }

        public static void SetNodeVisited(string node, bool visited)
        {
            if (string.IsNullOrEmpty(node))
            {
                Log.Warn("SetNodeVisited: node name is empty");
                return;
            }

            SetDialogueNodeVisited(node, visited);
            Log.Info($"SetNodeVisited: '{node}' -> {visited}");
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

            int functions = 0;
            int commands = 0;

            void Fn(Action register)
            {
                register();
                functions++;
            }

            void Cmd(Action register)
            {
                register();
                commands++;
            }

            Fn(() => runner.AddFunction<int>(YARN_FN_GET_LAST_GRID_ABERRATION_COUNT, GetLastGridAberrationCount));
            Fn(() => runner.AddFunction<bool>(YARN_FN_DISTINCT_ABERRATIONS_MET, AreDistinctAberrationsMet));
            Fn(() => runner.AddFunction<string, int>(YARN_FN_GET_LAST_GRID_ITEM_COUNT, GetLastGridItemCount));
            Fn(() => runner.AddFunction<bool>(YARN_FN_WAS_SUBMITTED, WasQuestGridSubmitted));
            Fn(() => runner.AddFunction<bool>(YARN_FN_HAS_COMPASS, HasCompass));

            Cmd(() => runner.AddCommandHandler<string, int>(YARN_CMD_SET_EXPECTED, SetExpectedItem));
            Cmd(() => runner.AddCommandHandler(YARN_CMD_CLEAR_EXPECTED, ClearExpectedItems));
            Cmd(() => runner.AddCommandHandler(YARN_CMD_SET_CONSUME_ABERRATIONS, SetConsumeAberrations));
            Cmd(() => runner.AddCommandHandler<int>(YARN_CMD_SET_REQUIRED_DISTINCT_ABERRATIONS, SetRequiredDistinctAberrations));
            Cmd(() => runner.AddCommandHandler(YARN_CMD_GRANT_COMPASS, GrantCompass));

            // Fn(() => runner.AddFunction<int>(YARN_FN_GET_CHARGE, GetInsanityCharge));
            // Fn(() => runner.AddFunction<int>(YARN_FN_GET_MAX_CHARGE, GetInsanityMaxCharge));
            // Fn(() => runner.AddFunction<bool>(YARN_FN_IS_CELL_FULL, IsInsanityCellFull));
            // Fn(() => runner.AddFunction<bool>(YARN_FN_IS_CELL_CRAFTED, IsInsanityCellCrafted));
            // Cmd(() => runner.AddCommandHandler<int>(YARN_CMD_ADD_CHARGE, AddInsanityCharge));
            // Cmd(() => runner.AddCommandHandler<int>(YARN_CMD_ADD_CHARGE_PER_ABERRATION, AddInsanityChargePerAberration));
            // Cmd(() => runner.AddCommandHandler(YARN_CMD_CRAFT_CELL, CraftInsanityCell));
            // Cmd(() => runner.AddCommandHandler<string>(YARN_CMD_LOG, LogFromYarn));

            Log.Info($"RegisterYarnBindings: registered {functions} functions + {commands} commands");
        }

        public static bool ShouldDredgeRenderLine(string[] tags)
        {
            if (USE_DREDGE_DIALOGUE_ALWAYS)
                return true;

            bool isOurNode = GetCurrentDialogueNode().StartsWith(PREFIX);
            bool hasDredgeTag = HasDialogueTag(tags, TAG_DREDGE_UI);
            bool hasInsanityTag = HasDialogueTag(tags, TAG_INSANITY_UI);

            return (!isOurNode && !hasInsanityTag) || (isOurNode && hasDredgeTag);
        }

        public static bool ShouldDredgeRenderOptions()
        {
            if (USE_DREDGE_DIALOGUE_ALWAYS)
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
