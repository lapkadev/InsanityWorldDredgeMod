using System.Collections.Generic;
using System.Linq;
using static InsanityWorldMod.Core.Constants;

namespace InsanityWorldMod.Core
{
    public static partial class Constants
    {
        public const string YARN_FN_GET_LAST_GRID_ABERRATION_COUNT = "lapkadev_get_last_grid_aberration_count";
        public const string YARN_FN_GET_LAST_GRID_ITEM_COUNT       = "lapkadev_get_last_grid_item_count";
        public const string YARN_FN_WAS_SUBMITTED                  = "lapkadev_was_submitted";

        public const string YARN_CMD_SET_EXPECTED                  = "lapkadev_set_expected";
        public const string YARN_CMD_CLEAR_EXPECTED                = "lapkadev_clear_expected";
        public const string YARN_CMD_SET_CONSUME_ABERRATIONS       = "lapkadev_set_consume_aberrations";
    }

    public static partial class G
    {
        public static QuestGridState QuestGrid = new QuestGridState();
    }

    public static partial class Funcs
    {
        public static bool IsModQuestGrid(string configName)
        {
            return configName != null && configName.StartsWith(PREFIX);
        }

        public static void SetExpectedItem(string id, int count)
        {
            G.QuestGrid.ExpectedItems[id] = count;
            Log.Debug($"SetExpectedItem: '{id}' x{count}");
        }

        public static void ClearExpectedItems()
        {
            G.QuestGrid.ExpectedItems.Clear();
            G.QuestGrid.ConsumeAberrations = false;
            Log.Debug("ClearExpectedItems: expectations and aberration consumption cleared");
        }

        public static void SetConsumeAberrations()
        {
            G.QuestGrid.ConsumeAberrations = true;
            Log.Debug("SetConsumeAberrations: aberrations will be consumed on submit");
        }

        public static int GetLastGridAberrationCount()
        {
            return G.QuestGrid.LastAberrationCount;
        }

        public static int GetLastGridItemCount(string id)
        {
            return G.QuestGrid.LastItemCounts.TryGetValue(id, out int count) ? count : 0;
        }

        public static bool WasQuestGridSubmitted()
        {
            return G.QuestGrid.WasSubmitted;
        }

        public static void OnQuestGridOpened()
        {
            G.QuestGrid.WasSubmitted = false;
            G.QuestGrid.LastItemCounts.Clear();
            G.QuestGrid.LastAberrationCount = 0;
        }

        public static void OnQuestGridSubmitted()
        {
            G.QuestGrid.WasSubmitted = true;
            Log.Info("OnQuestGridSubmitted: submit treated as Done");
        }

        public static bool[] ResolveQuestGridExit(QuestGridItem[] items)
        {
            var state = G.QuestGrid;
            var keep = new bool[items.Length];

            SnapshotQuestGrid(items);

            bool expectedFulfilled = true;
            if (state.WasSubmitted && state.ExpectedItems.Count > 0)
            {
                foreach (var expected in state.ExpectedItems)
                {
                    int have = items.Count(item => item.Id == expected.Key);
                    if (have < expected.Value)
                    {
                        expectedFulfilled = false;
                        break;
                    }
                }
            }

            bool consumeExpected = state.WasSubmitted && expectedFulfilled;

            var expectedConsumed = new Dictionary<string, int>();
            int returned = 0;
            int aberrationsConsumed = 0;

            for (int i = 0; i < items.Length; i++)
            {
                if (!state.WasSubmitted)
                {
                    keep[i] = true;
                    returned++;
                    continue;
                }

                bool consume = false;
                if (consumeExpected && state.ExpectedItems.TryGetValue(items[i].Id, out int expectedCount))
                {
                    expectedConsumed.TryGetValue(items[i].Id, out int already);
                    if (already < expectedCount)
                    {
                        consume = true;
                        expectedConsumed[items[i].Id] = already + 1;
                    }
                }

                if (!consume && state.ConsumeAberrations && items[i].IsAberration)
                {
                    consume = true;
                    aberrationsConsumed++;
                }

                if (consume)
                    continue;

                keep[i] = true;
                returned++;
            }

            Log.Info($"ResolveQuestGridExit: submitted={state.WasSubmitted}, expectedFulfilled={expectedFulfilled}, returned {returned}, consumedExpected {expectedConsumed.Sum(entry => entry.Value)}, consumedAberrations {aberrationsConsumed}");
            return keep;
        }

        public static void SnapshotQuestGrid(QuestGridItem[] items)
        {
            var state = G.QuestGrid;

            state.LastItemCounts.Clear();
            state.LastAberrationCount = 0;

            foreach (var item in items)
            {
                state.LastItemCounts.TryGetValue(item.Id, out int existing);
                state.LastItemCounts[item.Id] = existing + 1;

                if (item.IsAberration)
                    state.LastAberrationCount++;
            }
        }
    }

    public struct QuestGridItem
    {
        public string Id;
        public bool IsAberration;
    }

    public class QuestGridState
    {
        public readonly Dictionary<string, int> ExpectedItems = new Dictionary<string, int>();
        public readonly Dictionary<string, int> LastItemCounts = new Dictionary<string, int>();

        public bool ConsumeAberrations;
        public bool WasSubmitted;
        public int LastAberrationCount;
    }
}
