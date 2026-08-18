using System;
using UnityEngine;
using InsanityWorldMod.Core;
using Winch.Util;

namespace InsanityWorldMod.DredgeRuntime
{
    public static partial class G
    {
        internal static TeleportState Teleport = new TeleportState();
    }

    public static partial class Funcs
    {
        public static void AddHooksDocks()
        {
            DredgeHooks.MoveShipToDock = (dockId, slotIndex) =>
            {
                if (!CanStartTeleport())
                    return false;

                var target = GetDockTarget(dockId, slotIndex);
                if (target == null)
                    return false;

                BeginTeleport(target);
                return true;
            };

            DredgeHooks.CancelPendingTeleport = StopTeleport;

            DredgeHooks.GetLastDock = () =>
            {
                var saveData = G.DredgeGame?.SaveData;
                if (saveData == null)
                    return null;

                return new DockSlot { DockId = saveData.dockId, SlotIndex = saveData.dockSlotIndex };
            };
        }

        public static bool CanStartTeleport()
        {
            if (G.Teleport.IsRunning)
            {
                Log.Debug("CanStartTeleport: already teleporting, request ignored");
                return false;
            }

            var player = G.DredgePlayer;
            if (player == null)
            {
                Log.Warn("CanStartTeleport: Player is null");
                return false;
            }

            if (player.PlayerTeleport == null)
            {
                Log.Error("CanStartTeleport: Player.PlayerTeleport is null");
                return false;
            }

            return true;
        }

        public static DockTarget GetDockTarget(string dockId, int slotIndex)
        {
            var dock = DockUtil.GetDock(dockId);
            if (dock == null)
            {
                Log.Error($"GetDockTarget: dock '{dockId}' not found");
                return null;
            }

            var dockPoi = dock.GetComponentInChildren<DockPOI>();
            if (dockPoi == null || dockPoi.dockSlots == null || dockPoi.dockSlots.Length == 0)
            {
                Log.Error($"GetDockTarget: dock '{dockId}' has no DockPOI/dockSlots");
                return null;
            }

            if (slotIndex < 0 || slotIndex >= dockPoi.dockSlots.Length)
            {
                Log.Warn($"GetDockTarget: dock '{dockId}' slotIndex {slotIndex} out of range [0, {dockPoi.dockSlots.Length}), falling back to 0");
                slotIndex = 0;
            }

            return new DockTarget
            {
                DockId = dockId,
                Dock = dock,
                Slot = dockPoi.dockSlots[slotIndex],
                SlotIndex = slotIndex,
            };
        }

        public static void BeginTeleport(DockTarget target)
        {
            G.Teleport.IsRunning = true;
            G.Teleport.OnComplete = () => CompleteTeleport(target);

            G.DredgeGameEvents.OnTeleportComplete += G.Teleport.OnComplete;
            G.DredgePlayer.PlayerTeleport.Teleport(target.Slot.position, 0f, null);
        }

        public static void CompleteTeleport(DockTarget target)
        {
            UnsubscribeTeleport();

            G.DredgePlayer.transform.rotation = target.Slot.rotation;
            G.DredgePlayer.Dock(target.Dock, target.SlotIndex, false);
            G.Teleport.IsRunning = false;

            Log.Info($"CompleteTeleport: ship docked at '{target.DockId}' slot {target.SlotIndex} at {target.Slot.position}");
        }

        public static void StopTeleport()
        {
            UnsubscribeTeleport();

            if (G.Teleport.IsRunning)
            {
                Log.Debug("StopTeleport: clearing stuck teleport flag");
                G.Teleport.IsRunning = false;
            }
        }

        public static void UnsubscribeTeleport()
        {
            if (G.Teleport.OnComplete == null)
                return;

            if (G.DredgeGameEvents != null)
                G.DredgeGameEvents.OnTeleportComplete -= G.Teleport.OnComplete;

            G.Teleport.OnComplete = null;
        }
    }

    internal class TeleportState
    {
        public bool IsRunning;
        public Action OnComplete;
    }

    public class DockTarget
    {
        public string DockId;
        public Dock Dock;
        public Transform Slot;
        public int SlotIndex;
    }
}
