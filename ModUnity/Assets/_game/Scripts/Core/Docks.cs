using static InsanityWorldMod.Core.Constants;
using static InsanityWorldMod.Core.DredgeHooks;

namespace InsanityWorldMod.Core
{
    public static partial class Constants
    {
        public const string DEFAULT_RESTART_DOCK  = "dock.greater-marrow";
        public const string LOCALE_PLAYER_RESPAWN = "insanity.player.respawn";
    }

    public static partial class Funcs
    {
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
        /// Falls back to DEFAULT_RESTART_DOCK slot 0 if no dock has been visited yet.
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
    }

    public struct DockSlot
    {
        public string DockId;
        public int SlotIndex;
    }
}
