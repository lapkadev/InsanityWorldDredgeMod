using System.Collections.Generic;
using InsanityWorldMod.Core;

namespace InsanityWorldMod.DredgeRuntime
{
    public static partial class Funcs
    {
        public static void AddHooksItems()
        {
            DredgeHooks.RepairHullAll = () =>
            {
                var items = G.DredgeGame?.ItemManager;
                if (items == null)
                {
                    Log.Warn("Items: item manager is null");
                    return;
                }

                items.RepairHullDamage(free: true);
            };

            DredgeHooks.RepairItemsDurability = () =>
            {
                var items = G.DredgeGame?.ItemManager;
                if (items == null)
                {
                    Log.Warn("Items: item manager is null");
                    return;
                }

                items.RepairAllItemDurability();
            };

            DredgeHooks.RepairHull = count =>
            {
                var inventory = G.DredgeGame?.SaveData?.Inventory;
                if (inventory == null)
                {
                    Log.Warn("Items: inventory is null");
                    return;
                }

                var damage = inventory.spatialUnderlayItems
                    .FindAll(i => i.GetItemData<SpatialItemData>().itemType == ItemType.DAMAGE);

                int repaired = 0;
                for (int i = 0; i < damage.Count && repaired < count; i++)
                {
                    inventory.RemoveObjectFromGridData(damage[i], true);
                    repaired++;
                }

                if (repaired == 0)
                    return;

                var events = G.DredgeGameEvents;
                events.TriggerFocusedGridCellChanged(G.DredgeGame.GridManager.LastSelectedCell);
                events.TriggerOnPlayerDamageChanged();
                events.TriggerItemInventoryChanged(null);

                Log.Info($"Items: repaired {repaired} hull cell(s)");
            };

            DredgeHooks.DamageHull = count =>
            {
                var grid = G.DredgeGame?.GridManager;
                if (grid == null)
                {
                    Log.Warn("Items: grid manager is null");
                    return;
                }

                grid.AddDamageToInventory(count, -1, -1);
            };

            DredgeHooks.GetRegularFishIds = () =>
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
                    if (data != null && !data.IsAberration)
                        ids.Add(data.id);
                }

                return ids.ToArray();
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

            DredgeHooks.GiveItemToPlayer = itemId =>
            {
                var game = G.DredgeGame;
                if (game?.ItemManager == null || game.GridManager == null || game.SaveData == null)
                {
                    Log.Warn("Items: game is not ready");
                    return false;
                }

                var data = game.ItemManager.GetItemDataById<SpatialItemData>(itemId);
                if (data == null)
                {
                    Log.Warn($"Items: item '{itemId}' not found");
                    return false;
                }

                var instance = game.ItemManager.CreateItem<SpatialItemInstance>(data);
                if (instance == null)
                {
                    Log.Warn($"Items: item '{itemId}' not created");
                    return false;
                }

                return game.GridManager.AddItemInstanceToGrid(instance, true, game.SaveData.Inventory, game.SaveData.Storage);
            };

            DredgeHooks.GetEngineIds = () =>
            {
                var items = G.DredgeGame?.ItemManager;
                if (items == null)
                {
                    Log.Warn("Items: item manager is null");
                    return new string[0];
                }

                var ids = new List<string>();
                foreach (var data in items.GetAllItemsOfType<EngineItemData>())
                {
                    if (data != null)
                        ids.Add(data.id);
                }

                return ids.ToArray();
            };

            DredgeHooks.GetEngineSpeed = engineId =>
            {
                var data = G.DredgeGame?.ItemManager?.GetItemDataById<EngineItemData>(engineId);
                if (data == null)
                {
                    Log.Warn($"Items: engine '{engineId}' not found");
                    return 0f;
                }

                return data.speedBonus;
            };

            DredgeHooks.GetItemShape = itemId =>
            {
                var data = G.DredgeGame?.ItemManager?.GetItemDataById<SpatialItemData>(itemId);
                if (data == null || data.dimensions == null)
                {
                    Log.Warn($"Items: item '{itemId}' not found");
                    return "";
                }

                int width = 0;
                int height = 0;
                foreach (var cell in data.dimensions)
                {
                    if (cell.x + 1 > width)
                        width = cell.x + 1;

                    if (cell.y + 1 > height)
                        height = cell.y + 1;
                }

                return $"{width}x{height}, {data.dimensions.Count} cells";
            };

            DredgeHooks.GetItemName = itemId =>
            {
                var data = G.DredgeGame?.ItemManager?.GetItemDataById<ItemData>(itemId);
                if (data == null || data.itemNameKey == null || data.itemNameKey.IsEmpty)
                {
                    Log.Warn($"Items: item '{itemId}' has no name");
                    return "";
                }

                return data.itemNameKey.GetLocalizedString();
            };
        }
    }
}
