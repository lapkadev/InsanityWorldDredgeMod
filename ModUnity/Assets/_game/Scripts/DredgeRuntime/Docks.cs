using System.Collections.Generic;
using UnityEngine;
using InsanityWorldMod.Core;
using Winch.Util;

namespace InsanityWorldMod.DredgeRuntime
{
    public static partial class Funcs
    {
        public static void AddHooksDocks()
        {
            DredgeHooks.GetLastDock = () =>
            {
                var saveData = G.DredgeGame?.SaveData;
                if (saveData == null)
                    return null;

                return new DockSlot { DockId = saveData.dockId, SlotIndex = saveData.dockSlotIndex };
            };

            DredgeHooks.GetDockIds = () =>
            {
                var docks = DockUtil.GetAllDocks();
                var ids = new List<string>();
                foreach (var dock in docks)
                {
                    if (dock != null && dock.Data != null && !string.IsNullOrEmpty(dock.Data.Id))
                        ids.Add(dock.Data.Id);
                }

                return ids.ToArray();
            };

            DredgeHooks.GetDockName = dockId =>
            {
                var data = DockUtil.GetDockData(dockId);
                if (data == null || data.DockNameKey == null || data.DockNameKey.IsEmpty)
                {
                    Log.Warn($"Docks: dock '{dockId}' has no name");
                    return "";
                }

                return data.DockNameKey.GetLocalizedString();
            };
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

        public static void DockShipAt(DockTarget target)
        {
            G.DredgePlayer.transform.rotation = target.Slot.rotation;
            G.DredgePlayer.Dock(target.Dock, target.SlotIndex, false);

            Log.Info($"DockShipAt: ship docked at '{target.DockId}' slot {target.SlotIndex} at {target.Slot.position}");
        }
    }

    public class DockTarget
    {
        public string DockId;
        public Dock Dock;
        public Transform Slot;
        public int SlotIndex;
    }
}
