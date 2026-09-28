using System.Collections.Generic;
using System.IO;
using HarmonyLib;
using InsanityWorldMod.Core;
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
    }

    public static partial class Funcs
    {
        public static void AddHooksWinch()
        {
            Core.G.ModBasePath = ModAssemblyLoader.GetCurrentMod()?.BasePath;

            DredgeHooks.GetAllBundles = GetModBundles;
            DredgeHooks.GetInstalledMods = GetWinchEnabledMods;
            DredgeHooks.GetModsTabEntry = FindWinchModsTabEntry;
        }

        public static List<InstalledModInfo> GetWinchEnabledMods()
        {
            var result = new List<InstalledModInfo>();
            var mods = AccessTools.Field(typeof(ModAssemblyLoader), WINCH_ENABLED_MODS_FIELD)?.GetValue(null) as Dictionary<string, ModAssembly>;
            if (mods == null)
            {
                // Log.Warn($"GetWinchEnabledMods: {nameof(ModAssemblyLoader)}.{WINCH_ENABLED_MODS_FIELD} not found");
                return result;
            }

            foreach (var mod in mods.Values)
                result.Add(new InstalledModInfo { Guid = mod.GUID, Name = mod.Name, Version = mod.Version, Dir = mod.BasePath });

            return result;
        }

        public static RectTransform FindWinchModsTabEntry(string modGuid)
        {
            var type = AccessTools.TypeByName(WINCH_MODS_TAB_TYPE);
            var tab = type == null ? null : Traverse.Create(type).Property(WINCH_MODS_TAB_INSTANCE).GetValue();
            var list = tab == null ? null : Traverse.Create(tab).Field(WINCH_MODS_TAB_LIST_FIELD).GetValue<Transform>();
            if (list == null)
                return null;

            var entry = list.Find(modGuid + WINCH_MOD_LABEL_SUFFIX) ?? list.Find(modGuid + WINCH_MOD_BUTTON_SUFFIX);
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
