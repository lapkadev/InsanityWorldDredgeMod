using System.Collections.Generic;
using System.Linq;
using static InsanityWorldMod.Core.Constants;

namespace InsanityWorldMod.Core
{
    public static partial class Constants
    {
        public const string YARN_FN_GET_LAST_GRID_ABERRATION_COUNT = "insanity_get_last_grid_aberration_count";
        public const string YARN_FN_GET_LAST_GRID_ITEM_COUNT       = "insanity_get_last_grid_item_count";
        public const string YARN_FN_WAS_SUBMITTED                  = "insanity_was_submitted";
        public const string YARN_FN_DISTINCT_ABERRATIONS_MET       = "insanity_distinct_aberrations_met";

        public const string YARN_CMD_SET_EXPECTED                  = "insanity_set_expected";
        public const string YARN_CMD_CLEAR_EXPECTED                = "insanity_clear_expected";
        public const string YARN_CMD_SET_CONSUME_ABERRATIONS       = "insanity_set_consume_aberrations";
        public const string YARN_CMD_SET_REQUIRED_DISTINCT_ABERRATIONS = "insanity_set_required_distinct_aberrations";
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
            G.QuestGrid.RequiredDistinctAberrations = 0;
            Log.Debug("ClearExpectedItems: expectations and aberration consumption cleared");
        }

        public static void SetConsumeAberrations()
        {
            G.QuestGrid.ConsumeAberrations = true;
            Log.Debug("SetConsumeAberrations: aberrations will be consumed on submit");
        }

        public static void SetRequiredDistinctAberrations(int count)
        {
            G.QuestGrid.RequiredDistinctAberrations = count;
            Log.Debug($"SetRequiredDistinctAberrations: {count} distinct aberrations required");
        }

        public static bool AreDistinctAberrationsMet()
        {
            var state = G.QuestGrid;
            return state.RequiredDistinctAberrations > 0
                && state.LastDistinctAberrationCount >= state.RequiredDistinctAberrations;
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
            G.QuestGrid.LastDistinctAberrationCount = 0;
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

            bool distinctFulfilled = state.RequiredDistinctAberrations <= 0 || AreDistinctAberrationsMet();
            bool consumeAberrations = state.WasSubmitted && state.ConsumeAberrations && distinctFulfilled;

            var expectedConsumed = new Dictionary<string, int>();
            var aberrationsTaken = new HashSet<string>();
            int returned = 0;
            int aberrationsConsumed = 0;

            bool TakeAberration(string id)
            {
                if (state.RequiredDistinctAberrations <= 0)
                    return true;

                if (aberrationsTaken.Count >= state.RequiredDistinctAberrations)
                    return false;

                return aberrationsTaken.Add(id);
            }

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

                if (!consume && consumeAberrations && items[i].IsAberration && TakeAberration(items[i].Id))
                {
                    consume = true;
                    aberrationsConsumed++;
                }

                if (consume)
                    continue;

                keep[i] = true;
                returned++;
            }

            Log.Info($"ResolveQuestGridExit: submitted={state.WasSubmitted}, expectedFulfilled={expectedFulfilled}, distinctFulfilled={distinctFulfilled}, returned {returned}, consumedExpected {expectedConsumed.Sum(entry => entry.Value)}, consumedAberrations {aberrationsConsumed}");
            return keep;
        }

        public static void SnapshotQuestGrid(QuestGridItem[] items)
        {
            var state = G.QuestGrid;

            state.LastItemCounts.Clear();
            state.LastAberrationCount = 0;
            state.LastDistinctAberrationCount = 0;

            var distinctAberrations = new HashSet<string>();

            foreach (var item in items)
            {
                state.LastItemCounts.TryGetValue(item.Id, out int existing);
                state.LastItemCounts[item.Id] = existing + 1;

                if (!item.IsAberration)
                    continue;

                state.LastAberrationCount++;
                distinctAberrations.Add(item.Id);
            }

            state.LastDistinctAberrationCount = distinctAberrations.Count;
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
        public int RequiredDistinctAberrations;
        public int LastAberrationCount;
        public int LastDistinctAberrationCount;
    }
}
