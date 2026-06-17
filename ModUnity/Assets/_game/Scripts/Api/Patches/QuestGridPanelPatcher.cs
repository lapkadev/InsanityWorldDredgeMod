using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using InsanityWorldMod.Core;
using static InsanityWorldMod.Core.Constants;

namespace InsanityWorldMod.Api
{
    public static class QuestGridPanelPatcher
    {
        public static Dictionary<string, int> ExpectedItems = new();
        public static bool ConsumeAberrations;
        public static bool WasSubmittedByDone;
        public static Dictionary<string, int> LastGridItemCounts = new();
        public static int LastGridAberrationCount;

        [HarmonyPatch(typeof(QuestGridPanel), "OnShowFinish")]
        public static class OnShowFinishPatch
        {
            [HarmonyPostfix]
            public static void Postfix(QuestGridPanel __instance)
            {
                var config = AccessTools.Field(typeof(QuestGridPanel), "currentQuestGridConfig").GetValue(__instance) as QuestGridConfig;
                if (config == null || !config.name.StartsWith(PREFIX)) return;

                WasSubmittedByDone = false;
                LastGridItemCounts.Clear();
                LastGridAberrationCount = 0;
            }
        }

        [HarmonyPatch(typeof(QuestGridPanel), "OnHideStart")]
        public static class OnHideStartPatch
        {
            [HarmonyPrefix]
            public static void Prefix(QuestGridPanel __instance)
            {
                var config = AccessTools.Field(typeof(QuestGridPanel), "currentQuestGridConfig").GetValue(__instance) as QuestGridConfig;
                if (config == null || !config.name.StartsWith(PREFIX)) return;

                var grid = AccessTools.Field(typeof(QuestGridPanel), "currentGrid").GetValue(__instance) as SerializableGrid;
                if (grid == null) return;

                SnapshotCounts(grid);
                ProcessItemsForExit(grid);
            }
        }

        [HarmonyPatch(typeof(ControlPromptEntryUI), "OnPointerDown")]
        public static class ControlPromptOnPointerDownPatch
        {
            [HarmonyPrefix]
            public static void Prefix(ControlPromptEntryUI __instance)
            {
                var panel = GameManager.Instance?.UI?.QuestGridPanel;
                if (panel == null || !panel.gameObject.activeSelf) return;

                var config = AccessTools.Field(typeof(QuestGridPanel), "currentQuestGridConfig").GetValue(panel) as QuestGridConfig;
                if (config == null || !config.name.StartsWith(PREFIX)) return;

                var exitPrompt = AccessTools.Field(typeof(QuestGridPanel), "exitControlPromptUI").GetValue(panel) as ControlPromptEntryUI;
                if (exitPrompt != __instance) return;

                G.Log.Info("QuestGridPanelPatcher: vanilla exit prompt clicked - treating as Done");
                WasSubmittedByDone = true;
            }
        }

        private static void SnapshotCounts(SerializableGrid grid)
        {
            LastGridItemCounts.Clear();
            LastGridAberrationCount = 0;
            foreach (var item in grid.spatialItems)
            {
                LastGridItemCounts.TryGetValue(item.id, out int existing);
                LastGridItemCounts[item.id] = existing + 1;
                var fishData = item.GetItemData<SpatialItemData>() as FishItemData;
                if (fishData != null && fishData.IsAberration) LastGridAberrationCount++;
            }
        }

        private static void ProcessItemsForExit(SerializableGrid grid)
        {
            var inventory = GameManager.Instance.SaveData.Inventory;
            var storage = GameManager.Instance.SaveData.Storage;

            bool expectedFulfilled = true;
            if (WasSubmittedByDone && ExpectedItems.Count > 0)
            {
                foreach (var kv in ExpectedItems)
                {
                    int have = grid.spatialItems.Count(i => i.id == kv.Key);
                    if (have < kv.Value) { expectedFulfilled = false; break; }
                }
            }
            bool consumeExpected = WasSubmittedByDone && expectedFulfilled;

            var itemsToReturn = new List<SpatialItemInstance>();
            var expectedConsumed = new Dictionary<string, int>();
            int aberrationsConsumed = 0;

            foreach (var item in grid.spatialItems)
            {
                if (!WasSubmittedByDone) { itemsToReturn.Add(item); continue; }

                bool consume = false;
                if (consumeExpected && ExpectedItems.TryGetValue(item.id, out int expectedCount))
                {
                    expectedConsumed.TryGetValue(item.id, out int already);
                    if (already < expectedCount)
                    {
                        consume = true;
                        expectedConsumed[item.id] = already + 1;
                    }
                }
                if (!consume && ConsumeAberrations)
                {
                    var fishData = item.GetItemData<SpatialItemData>() as FishItemData;
                    if (fishData != null && fishData.IsAberration) { consume = true; aberrationsConsumed++; }
                }
                if (!consume) itemsToReturn.Add(item);
            }

            foreach (var item in itemsToReturn)
            {
                try { GameManager.Instance.GridManager.AddItemInstanceToGrid(item, true, inventory, storage); }
                catch (Exception ex) { G.Log.Error($"QuestGridPanelPatcher: failed to return item '{item.id}': {ex.Message}"); }
            }
            grid.spatialItems.Clear();

            G.Log.Info($"QuestGridPanelPatcher: submitted={WasSubmittedByDone}, expectedFulfilled={expectedFulfilled}, returned {itemsToReturn.Count}, consumedExpected {expectedConsumed.Sum(kv => kv.Value)}, consumedAberrations {aberrationsConsumed}");
        }
    }
}
