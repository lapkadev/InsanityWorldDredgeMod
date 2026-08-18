using System;
using HarmonyLib;
using InsanityWorldMod.Core;
using static InsanityWorldMod.Core.Funcs;

namespace InsanityWorldMod.DredgeRuntime
{
    public static class QuestGridPanelPatcher
    {
        [HarmonyPatch(typeof(QuestGridPanel), "OnShowFinish")]
        public static class OnShowFinishPatch
        {
            [HarmonyPostfix]
            public static void Postfix(QuestGridPanel __instance)
            {
                var config = AccessTools.Field(typeof(QuestGridPanel), "currentQuestGridConfig").GetValue(__instance) as QuestGridConfig;
                if (config == null || !IsModQuestGrid(config.name))
                    return;

                OnQuestGridOpened();
            }
        }

        [HarmonyPatch(typeof(QuestGridPanel), "OnHideStart")]
        public static class OnHideStartPatch
        {
            [HarmonyPrefix]
            public static void Prefix(QuestGridPanel __instance)
            {
                var config = AccessTools.Field(typeof(QuestGridPanel), "currentQuestGridConfig").GetValue(__instance) as QuestGridConfig;
                if (config == null || !IsModQuestGrid(config.name))
                    return;

                var grid = AccessTools.Field(typeof(QuestGridPanel), "currentGrid").GetValue(__instance) as SerializableGrid;
                if (grid == null)
                    return;

                ApplyGridExit(grid);
            }
        }

        [HarmonyPatch(typeof(ControlPromptEntryUI), "OnPointerDown")]
        public static class ControlPromptOnPointerDownPatch
        {
            [HarmonyPrefix]
            public static void Prefix(ControlPromptEntryUI __instance)
            {
                var panel = G.DredgeGame?.UI?.QuestGridPanel;
                if (panel == null || !panel.gameObject.activeSelf)
                    return;

                var config = AccessTools.Field(typeof(QuestGridPanel), "currentQuestGridConfig").GetValue(panel) as QuestGridConfig;
                if (config == null || !IsModQuestGrid(config.name))
                    return;

                var exitPrompt = AccessTools.Field(typeof(QuestGridPanel), "exitControlPromptUI").GetValue(panel) as ControlPromptEntryUI;
                if (exitPrompt != __instance)
                    return;

                OnQuestGridSubmitted();
            }
        }

        private static void ApplyGridExit(SerializableGrid grid)
        {
            var instances = grid.spatialItems.ToArray();
            var items = new QuestGridItem[instances.Length];

            for (int i = 0; i < instances.Length; i++)
            {
                var fishData = instances[i].GetItemData<SpatialItemData>() as FishItemData;
                items[i] = new QuestGridItem
                {
                    Id = instances[i].id,
                    IsAberration = fishData != null && fishData.IsAberration,
                };
            }

            var keep = ResolveQuestGridExit(items);

            var inventory = G.DredgeGame.SaveData.Inventory;
            var storage = G.DredgeGame.SaveData.Storage;

            for (int i = 0; i < instances.Length; i++)
            {
                if (!keep[i])
                    continue;

                try
                {
                    G.DredgeGame.GridManager.AddItemInstanceToGrid(instances[i], true, inventory, storage);
                }
                catch (Exception ex)
                {
                    Log.Error($"QuestGridPanelPatcher: failed to return item '{instances[i].id}': {ex.Message}");
                }
            }

            grid.spatialItems.Clear();
        }
    }
}
