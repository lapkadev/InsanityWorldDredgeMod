using System;
using System.Collections.Generic;
using UnityEngine;

namespace InsanityWorldMod.Core
{
    public static partial class G
    {
        public static RectTransform ModsTabPanel;
        public static Action ModsListShown;
    }

    public static partial class Funcs
    {
        public static RectTransform GetModsMenuButton()
        {
            return DredgeHooks.GetModsMenuButton();
        }

        public static List<InstalledModInfo> GetInstalledMods()
        {
            var mods = DredgeHooks.GetInstalledMods();
            // Log.Info($"GetInstalledMods: {mods.Count} enabled mod(s)");
            return mods;
        }

        public static RectTransform GetModsTabEntry(string modGuid)
        {
            if (string.IsNullOrEmpty(modGuid))
            {
                // Log.Warn("GetModsTabEntry: mod guid is empty");
                return null;
            }

            return DredgeHooks.GetModsTabEntry(modGuid);
        }

        public static void OnModsListShown(RectTransform panel)
        {
            G.ModsTabPanel = panel;
            // Log.Debug("OnModsListShown: mods list rebuilt");
            G.ModsListShown?.Invoke();
        }
    }

    public class InstalledModInfo
    {
        public string Guid;
        public string Name;
        public string Version;
        public string Dir;
    }
}
