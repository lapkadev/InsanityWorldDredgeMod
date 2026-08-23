using System.Collections.Generic;
using InsanityWorldMod.Core;

namespace InsanityWorldMod.DredgeRuntime
{
    public static partial class Funcs
    {
        public static void AddHooksItems()
        {
            DredgeHooks.RepairHull = () =>
            {
                var items = G.DredgeGame?.ItemManager;
                if (items == null)
                {
                    Log.Warn("Items: item manager is null");
                    return;
                }

                items.RepairHullDamage(free: true);
            };

            DredgeHooks.RepairAllItems = () =>
            {
                var items = G.DredgeGame?.ItemManager;
                if (items == null)
                {
                    Log.Warn("Items: item manager is null");
                    return;
                }

                items.RepairAllItemDurability();
            };

            DredgeHooks.GetAberrationFishIds = () =>
            {
                var items = G.DredgeGame?.ItemManager;
                if (items == null)
                {
                    Log.Warn("Items: item manager is null");
                    return new string[0];
                }

                var ids = new List<string>();
                foreach (var data in items.GetAllItemsOfType<FishItemData>())
                {
                    if (data != null && data.IsAberration)
                        ids.Add(data.id);
                }

                return ids.ToArray();
            };

            DredgeHooks.GiveFishToPlayer = fishId =>
            {
                var game = G.DredgeGame;
                if (game?.ItemManager == null || game.GridManager == null || game.SaveData == null)
                {
                    Log.Warn("Items: game is not ready");
                    return false;
                }

                if (game.ItemManager.GetItemDataById<FishItemData>(fishId) == null)
                {
                    Log.Warn($"Items: fish '{fishId}' not found");
                    return false;
                }

                var instance = new FishItemInstance
                {
                    id = fishId,
                    size = 0.5f,
                    freshness = game.GameConfigData.MaxFreshness,
                };

                return game.GridManager.AddItemInstanceToGrid(instance, true, game.SaveData.Inventory, game.SaveData.Storage);
            };
        }
    }
}
