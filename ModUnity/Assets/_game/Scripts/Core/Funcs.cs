using System;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;
using static InsanityWorldMod.Core.Constants;
using static InsanityWorldMod.Core.DredgeHooks;

namespace InsanityWorldMod.Core
{
    public static partial class Constants
    {
        public const string LOCALE_PLAYER_RESPAWN = "insanity.player.respawn";
    }

    public static partial class Funcs
    {
        // ===== Dock / teleport =====

        /// <summary>
        /// Teleports the ship to a specific dock + slot. Used by Restart actions.
        /// </summary>
        /// <param name="dockId">Vanilla dock id (e.g. "dock.greater-marrow").</param>
        /// <param name="slotIndex">Dock slot index. Out-of-range values are clamped to 0 with a warning.</param>
        public static void TeleportShipToDock(string dockId, int slotIndex = 0)
        {
            if (!MoveShipToDock(dockId, slotIndex))
                return;

            ShowNotification(NotificationKind.SPOOKY_EVENT, LOCALE_PLAYER_RESPAWN, NotificationColor.EMPHASIS);
        }

        /// <summary>
        /// Teleports the ship to the LAST dock the player was parked at 
        /// (vanilla DREDGE tracks this in `SaveData.dockId` + `SaveData.dockSlotIndex`, updated on each `Player.Dock(...)` call). 
        /// Falls back to "Constants.DEFAULT_RESTART_DOCK" slot 0 if no dock has been visited yet.
        /// </summary>
        public static void TeleportToLastDock()
        {
            var lastDock = GetLastDock();

            if (lastDock == null || string.IsNullOrEmpty(lastDock.Value.DockId))
            {
                Log.Info($"TeleportToLastDock: no last dock recorded, falling back to '{DEFAULT_RESTART_DOCK}' slot 0");
                TeleportShipToDock(DEFAULT_RESTART_DOCK, 0);
                return;
            }

            TeleportShipToDock(lastDock.Value.DockId, lastDock.Value.SlotIndex);
        }

        /// <summary>
        /// Resets transient operation flags that should not survive across game-load cycles.
        /// </summary>
        public static void ResetTransientState()
        {
            CancelPendingTeleport();
        }

        // ===== Run lifecycle =====

        public static void StartNewRun()
        {
            G.Run = new RunState();
            if (G.Save != null) G.Save.TotalRuns++;
            Log.Info($"StartNewRun: run #{G.Save?.TotalRuns}");
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
            RepairHull();
            RepairAllItems();
            Log.Debug("RepairFull: hull + all items repaired");
        }

        // ===== Save / Load =====

        public static void Save()
        {
            if (G.Game == null || G.Save == null) { Log.Warn("Save: state not initialized"); return; }

            G.Game.CaptureFromVanilla();

            var slot = ResolveSlot("last");
            if (slot < 0) { Log.Debug("Save: no active slot yet, skipping"); return; }

            try
            {
                var path = GetSaveFilePath(slot);
                var json = JsonConvert.SerializeObject(G.Save, Formatting.Indented);
                File.WriteAllText(path, json);
                Log.Debug($"Save: slot={slot} -> {path}");
            }
            catch (Exception ex)
            {
                Log.Error($"Save: failed to write slot {slot}: {ex}");
            }
        }

        public static void Load(string save = "last")
        {
            var slot = ResolveSlot(save);
            if (slot < 0) { Log.Warn($"Load: cannot resolve slot from '{save}'"); return; }

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
                    Log.Error($"Load: failed to parse {path}, using default: {ex}");
                }
            }

            G.Save = token == null
                ? new SaveState()
                : SaveStateMigrator.MigrateAndDeserialize(token);

            G.Game = new GameState();
            G.Game.InitFromSave();
            G.Game.ApplyToVanilla();

            Log.Info($"Load: slot={slot}, TotalRuns={G.Save.TotalRuns}, TotalDeathsIntercepted={G.Save.TotalDeathsIntercepted}");
        }

        public static string GetSaveFilePath(int slot)
        {
            string dir = USE_DEBUG_PATH
                ? Path.Combine(G.ModBasePath, "saves")
                : Path.Combine(Application.persistentDataPath, "InsanityWorldMod", "saves");

            Directory.CreateDirectory(dir);
            return Path.Combine(dir, $"slot_{slot}.json");
        }

        private static int ResolveSlot(string save)
        {
            if (save == "last")
                return GetActiveSaveSlot();
            return int.TryParse(save, out var n) ? n : -1;
        }
    }

    public struct DockSlot
    {
        public string DockId;
        public int SlotIndex;
    }
}
