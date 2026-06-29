using System;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;
using static InsanityWorldMod.Core.Constants;
using static InsanityWorldMod.Core.DredgeHooks;

namespace InsanityWorldMod.Core
{
    /// <summary>
    /// Core-side public functions (gameplay logic: Save/Load/Run lifecycle, teleport, etc.).
    /// Partial - can be split across multiple files within Core.
    /// </summary>
    public static partial class Funcs
    {
        // Transient flag - true while a teleport is in flight (between Teleport() call
        // and OnTeleportComplete fire). Static, so it survives scene reloads - which is
        // a hazard: if the player exits to main menu mid-teleport, OnTeleportComplete
        // may never fire and the flag stays stuck true forever. ResetTransientState()
        // (called from GameController.OnGameLoaded) clears it on every save reload.
        private static bool _isTeleporting;
        private static Action _pendingTeleportCallback;

        // ===== Dock / teleport =====

        /// <summary>
        /// Teleports the ship to a specific dock + slot. Used by Restart actions.
        /// </summary>
        /// <param name="dockId">Vanilla dock id (e.g. "dock.greater-marrow").</param>
        /// <param name="slotIndex">Dock slot index. Out-of-range values are clamped to 0 with a warning.</param>
        public static void TeleportShipToDock(string dockId, int slotIndex = 0)
        {
            if (_isTeleporting) { G.Log.Debug("TeleportShipToDock: already teleporting, click ignored."); return; }
            if (G.Player == null) { G.Log.Warn("TeleportShipToDock: Player is null"); return; }
            if (G.Player.PlayerTeleport == null) { G.Log.Error("TeleportShipToDock: Player.PlayerTeleport is null"); return; }

            var dock = FindDockById(dockId);
            if (dock == null) { G.Log.Error($"TeleportShipToDock: dock '{dockId}' not found"); return; }

            var dockPoi = dock.GetComponentInChildren<DockPOI>();
            if (dockPoi == null || dockPoi.dockSlots == null || dockPoi.dockSlots.Length == 0)
            {
                G.Log.Error($"TeleportShipToDock: dock '{dockId}' has no DockPOI/dockSlots");
                return;
            }

            if (slotIndex < 0 || slotIndex >= dockPoi.dockSlots.Length)
            {
                G.Log.Warn($"TeleportShipToDock: dock '{dockId}' slotIndex {slotIndex} out of range [0, {dockPoi.dockSlots.Length}), falling back to 0");
                slotIndex = 0;
            }

            var slot = dockPoi.dockSlots[slotIndex];
            var resolvedSlotIndex = slotIndex;

            _isTeleporting = true;

            // Store as field so ResetTransientState() can explicitly unsubscribe
            // if the player exits to menu before OnTeleportComplete fires.
            Action callback = null;
            callback = () =>
            {
                GameEvents.Instance.OnTeleportComplete -= callback;
                if (_pendingTeleportCallback == callback) _pendingTeleportCallback = null;
                G.Player.transform.rotation = slot.rotation;
                G.Player.Dock(dock, resolvedSlotIndex, false);
                _isTeleporting = false;
                G.Log.Info($"Teleported ship to '{dockId}' slot {resolvedSlotIndex} at {slot.position}");
            };
            _pendingTeleportCallback = callback;

            GameEvents.Instance.OnTeleportComplete += callback;
            G.Player.PlayerTeleport.Teleport(slot.position, 0f, null);

            G.UI?.ShowNotificationWithColor(
                NotificationType.SPOOKY_EVENT,
                "insanity.player.respawn",
                G.Lang.GetColorCode(DredgeColorTypeEnum.EMPHASIS)
            );
        }

        /// <summary>
        /// Teleports the ship to the LAST dock the player was parked at 
        /// (vanilla DREDGE tracks this in `SaveData.dockId` + `SaveData.dockSlotIndex`, updated on each `Player.Dock(...)` call). 
        /// Falls back to "Constants.DEFAULT_RESTART_DOCK" slot 0 if no dock has been visited yet.
        /// </summary>
        public static void TeleportToLastDock()
        {
            var saveData = G.SaveVanilla;
            var lastDockId = saveData?.dockId;
            var lastSlotIndex = saveData?.dockSlotIndex ?? 0;

            if (string.IsNullOrEmpty(lastDockId))
            {
                G.Log.Info($"TeleportToLastDock: no last dock in SaveData, falling back to '{DEFAULT_RESTART_DOCK}' slot 0");
                TeleportShipToDock(DEFAULT_RESTART_DOCK, 0);
                return;
            }

            TeleportShipToDock(lastDockId, lastSlotIndex);
        }

        /// <summary>
        /// Resets transient operation flags that should not survive across game-load cycles.
        /// </summary>
        public static void ResetTransientState()
        {
            if (_pendingTeleportCallback != null)
            {
                if (GameEvents.Instance != null)
                    GameEvents.Instance.OnTeleportComplete -= _pendingTeleportCallback;
                _pendingTeleportCallback = null;
            }
            if (_isTeleporting)
            {
                G.Log.Debug("ResetTransientState: clearing stuck _isTeleporting flag");
                _isTeleporting = false;
            }
        }

        // ===== Run lifecycle =====

        public static void StartNewRun()
        {
            G.Run = new RunState();
            if (G.Save != null) G.Save.TotalRuns++;
            G.Log.Info($"StartNewRun: run #{G.Save?.TotalRuns}");
        }

        public static void OnDeathIntercepted()
        {
            if (G.Run == null) { StartNewRun(); return; }
            if (G.Save != null) G.Save.TotalDeathsIntercepted++;

            Save();
            StartNewRun();
        }

        public static void RepairFull()
        {
            if (G.GameVanilla?.ItemManager == null) { G.Log.Warn("RepairFull: ItemManager is null"); return; }
            G.GameVanilla.ItemManager.RepairHullDamage(free: true);
            G.GameVanilla.ItemManager.RepairAllItemDurability();
            G.Log.Debug("RepairFull: hull + all items repaired");
        }

        // ===== Save / Load =====

        public static void Save()
        {
            if (G.Game == null || G.Save == null) { G.Log.Warn("Save: state not initialized"); return; }

            G.Game.CaptureFromVanilla();

            var slot = ResolveSlot("last");
            if (slot < 0) { G.Log.Debug("Save: no active slot yet, skipping"); return; }

            try
            {
                var path = GetSaveFilePath(slot);
                var json = JsonConvert.SerializeObject(G.Save, Formatting.Indented);
                File.WriteAllText(path, json);
                G.Log.Debug($"Save: slot={slot} -> {path}");
            }
            catch (Exception ex)
            {
                G.Log.Error($"Save: failed to write slot {slot}: {ex}");
            }
        }

        public static void Load(string save = "last")
        {
            var slot = ResolveSlot(save);
            if (slot < 0) { G.Log.Warn($"Load: cannot resolve slot from '{save}'"); return; }

            JToken token = null;
            var path = GetSaveFilePath(slot);
            if (File.Exists(path))
            {
                try
                {
                    var json = File.ReadAllText(path);
                    token = JToken.Parse(json);
                }
                catch (Exception ex)
                {
                    G.Log.Error($"Load: failed to parse {path}, using default: {ex}");
                }
            }

            G.Save = token == null
                ? new SaveState()
                : SaveStateMigrator.MigrateAndDeserialize(token);

            G.Game = new GameState();
            G.Game.InitFromSave();
            G.Game.ApplyToVanilla();

            G.Log.Info($"Load: slot={slot}, TotalRuns={G.Save.TotalRuns}, TotalDeathsIntercepted={G.Save.TotalDeathsIntercepted}");
        }

        public static string GetSaveFilePath(int slot)
        {
            string dir = USE_DEBUG_PATH
                ? Path.Combine(GetModBasePath(), "saves")
                : Path.Combine(Application.persistentDataPath, "InsanityWorldMod", "saves");

            Directory.CreateDirectory(dir);
            return Path.Combine(dir, $"slot_{slot}.json");
        }

        private static int ResolveSlot(string save)
        {
            if (save == "last")
                return G.GameVanilla?.SaveManager?.ActiveSettingsData?.lastSaveSlot ?? -1;
            return int.TryParse(save, out var n) ? n : -1;
        }
    }
}
