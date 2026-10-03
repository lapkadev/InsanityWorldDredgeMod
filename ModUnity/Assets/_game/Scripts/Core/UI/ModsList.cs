using System;
using System.Collections.Generic;
using TMPro;
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

        public static TMP_Text GetModsTabTitle()
        {
            return DredgeHooks.GetModsTabTitle();
        }

        public static RectTransform GetModsTabListArea()
        {
            return DredgeHooks.GetModsTabListArea();
        }

        public static void SetModsTabShortcutsEnabled(bool isEnabled)
        {
            DredgeHooks.SetModsTabShortcutsEnabled(isEnabled);
        }

        public static bool IsModEnabled(string modGuid)
        {
            try
            {
                return DredgeHooks.IsModEnabled(modGuid);
            }
            catch (Exception ex)
            {
                // Log.Warn($"IsModEnabled: failed to read state of '{modGuid}': {ex.Message}");
                return true;
            }
        }

        public static bool SetModEnabled(string modGuid, bool isEnabled)
        {
            try
            {
                DredgeHooks.SetModEnabled(modGuid, isEnabled);
                return true;
            }
            catch (Exception ex)
            {
                // Log.Warn($"SetModEnabled: failed to save state of '{modGuid}': {ex.Message}");
                return false;
            }
        }

        public static string GetModsDir()
        {
            return DredgeHooks.GetModsDir();
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
        public bool IsLoaded;
    }
}
