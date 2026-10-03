using System.Collections.Generic;
using System.IO;
using HarmonyLib;
using InsanityWorldMod.Core;
using Newtonsoft.Json;
using TMPro;
using UnityEngine;
using Winch.Core;
using Winch.Util;
using static InsanityWorldMod.DredgeRuntime.Constants;

namespace InsanityWorldMod.DredgeRuntime
{
    public static partial class Constants
    {
        public const string MOD_BUNDLES_FOLDER = "Bundles";
        public const string MOD_BUNDLES_SUBDIR = "Assets/" + MOD_BUNDLES_FOLDER;
        public const string WINCH_ENABLED_MODS_FIELD = "EnabledModAssemblies";
        public const string WINCH_INSTALLED_MODS_FIELD = "_installedAssemblies";
        public const string WINCH_MODS_TAB_TITLE_FIELD = "headerTextLocalized";
        public const string WINCH_MODS_TAB_LIST_AREA_FIELD = "listScroller";
        public const string WINCH_MODS_TAB_DIALOG_FIELD = "settingsDialog";
        public const string SETTINGS_DIALOG_TABS_FIELD = "dialog";
        public const string WINCH_OWN_BUTTON_NAME = "WinchButton";
        public const string WINCH_OWN_GUID = "hacktix.winch";
    }

    public static partial class Funcs
    {
        public static void AddHooksWinch()
        {
            Core.G.ModBasePath = ModAssemblyLoader.GetCurrentMod()?.BasePath;

            DredgeHooks.GetAllBundles = GetModBundles;
            DredgeHooks.GetInstalledMods = GetWinchInstalledMods;
            DredgeHooks.GetModsTabEntry = FindWinchModsTabEntry;
            DredgeHooks.GetModsTabTitle = FindWinchModsTabTitle;
            DredgeHooks.GetModsTabListArea = FindWinchModsTabListArea;
            DredgeHooks.SetModsTabShortcutsEnabled = SetWinchModsTabShortcutsEnabled;
            DredgeHooks.IsModEnabled = IsWinchModEnabled;
            DredgeHooks.SetModEnabled = SetWinchModEnabled;
            DredgeHooks.GetModsDir = () => Paths.ModsPath;
        }

        public static TMP_Text FindWinchModsTabTitle()
        {
            var tab = GetWinchModsTab();
            var title = tab == null ? null : Traverse.Create(tab).Field(WINCH_MODS_TAB_TITLE_FIELD).GetValue<Component>();
            return title == null ? null : title.GetComponentInChildren<TMP_Text>(true);
        }

        public static RectTransform FindWinchModsTabListArea()
        {
            var tab = GetWinchModsTab();
            var area = tab == null ? null : Traverse.Create(tab).Field(WINCH_MODS_TAB_LIST_AREA_FIELD).GetValue<Component>();
            return area == null ? null : area.transform as RectTransform;
        }

        public static void SetWinchModsTabShortcutsEnabled(bool isEnabled)
        {
            var tab = GetWinchModsTab();
            var dialog = tab == null ? null : Traverse.Create(tab).Field(WINCH_MODS_TAB_DIALOG_FIELD).GetValue();
            var container = dialog == null ? null : Traverse.Create(dialog).Field(SETTINGS_DIALOG_TABS_FIELD).GetValue<TabbedPanelContainer>();
            if (container == null || !container.gameObject.activeInHierarchy)
                return;

            if (isEnabled)
                container.EnableTabShortcuts();
            else
                container.DisableTabShortcuts();
        }

        public static object GetWinchModsTab()
        {
            var type = AccessTools.TypeByName(WINCH_MODS_TAB_TYPE);
            return type == null ? null : Traverse.Create(type).Property(WINCH_MODS_TAB_INSTANCE).GetValue();
        }

        public static List<InstalledModInfo> GetWinchInstalledMods()
        {
            var result = new List<InstalledModInfo>();
            var mods = AccessTools.Field(typeof(ModAssemblyLoader), WINCH_INSTALLED_MODS_FIELD)?.GetValue(null) as Dictionary<string, ModAssembly>;
            var enabled = AccessTools.Field(typeof(ModAssemblyLoader), WINCH_ENABLED_MODS_FIELD)?.GetValue(null) as Dictionary<string, ModAssembly>;
            if (mods == null || enabled == null)
            {
                // Log.Warn($"GetWinchInstalledMods: {nameof(ModAssemblyLoader)} mod lists not found");
                return result;
            }

            var enabledGuids = new HashSet<string>();
            foreach (var mod in enabled.Values)
                enabledGuids.Add(mod.GUID);

            foreach (var mod in mods.Values)
            {
                result.Add(new InstalledModInfo
                {
                    Guid = mod.GUID,
                    Name = mod.Name,
                    Version = mod.Version,
                    Dir = mod.BasePath,
                    IsLoaded = enabledGuids.Contains(mod.GUID),
                });
            }

            return result;
        }

        public static bool IsWinchModEnabled(string modGuid)
        {
            var states = ReadWinchModList();
            return !states.TryGetValue(modGuid, out var isEnabled) || isEnabled;
        }

        public static void SetWinchModEnabled(string modGuid, bool isEnabled)
        {
            var states = ReadWinchModList();
            states[modGuid] = isEnabled;
            File.WriteAllText(Paths.ModListPath, JsonConvert.SerializeObject(states, Formatting.Indented));
        }

        public static Dictionary<string, bool> ReadWinchModList()
        {
            var path = Paths.ModListPath;
            if (!File.Exists(path))
                return new Dictionary<string, bool>();

            return JsonConvert.DeserializeObject<Dictionary<string, bool>>(File.ReadAllText(path)) ?? new Dictionary<string, bool>();
        }

        public static RectTransform FindWinchModsTabEntry(string modGuid)
        {
            var tab = GetWinchModsTab();
            var list = tab == null ? null : Traverse.Create(tab).Field(WINCH_MODS_TAB_LIST_FIELD).GetValue<Transform>();
            if (list == null)
                return null;

            var entry = list.Find(modGuid + WINCH_MOD_LABEL_SUFFIX) ?? list.Find(modGuid + WINCH_MOD_BUTTON_SUFFIX);
            if (entry == null && modGuid == WINCH_OWN_GUID)
                entry = list.Find(WINCH_OWN_BUTTON_NAME);

            return entry as RectTransform;
        }

        public static IEnumerable<AssetBundle> GetModBundles()
        {
            var bundles = new List<AssetBundle>();

            var dir = Path.Combine(Core.G.ModBasePath, MOD_BUNDLES_SUBDIR);
            if (!Directory.Exists(dir))
            {
                Log.Warn($"GetModBundles: bundles folder not found at '{dir}'");
                return bundles;
            }

            foreach (var file in Directory.GetFiles(dir))
            {
                var key = Path.GetFileName(file);
                if (key == MOD_BUNDLES_FOLDER)
                    continue;

                if (AssetBundleUtil.AssetBundles.TryGetValue(key, out var bundle))
                    bundles.Add(bundle);
            }

            Log.Info($"GetModBundles: {bundles.Count} of {AssetBundleUtil.AssetBundles.Count} loaded bundles belong to the mod");
            return bundles;
        }
    }
}
