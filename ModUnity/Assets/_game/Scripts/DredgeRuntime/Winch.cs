using System.Collections.Generic;
using System.IO;
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
    }

    public static partial class Funcs
    {
        public static void AddHooksWinch()
        {
            Core.G.ModBasePath = ModAssemblyLoader.GetCurrentMod()?.BasePath;

            DredgeHooks.GetAllBundles = GetModBundles;
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
