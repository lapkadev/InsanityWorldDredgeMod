using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace InsanityWorldMod.Core
{
    public static partial class Constants
    {
        public const string BUNDLES_SUBDIR = "Assets/Bundles";
    }

    public static partial class G
    {
        public static Dictionary<string, GameObject>    Prefabs { get; } = new Dictionary<string, GameObject>();
        public static Dictionary<string, TMP_FontAsset> Fonts   { get; } = new Dictionary<string, TMP_FontAsset>();
    }

    public static partial class Funcs
    {
        public static void LoadAll(AssetBundle bundle)
        {
            if (bundle == null)
                return;

            LoadPrefabs(bundle);
            LoadFonts(bundle);
        }

        public static void LoadPrefabs(AssetBundle bundle)
        {
            var assets = bundle.LoadAllAssets<GameObject>();
            foreach (var asset in assets)
                G.Prefabs[asset.name] = asset;
            G.Log.Info($"LoadPrefabs: cached {assets.Length} from '{bundle.name}'");
        }

        public static void LoadFonts(AssetBundle bundle)
        {
            var assets = bundle.LoadAllAssets<TMP_FontAsset>();
            foreach (var asset in assets)
                G.Fonts[asset.name] = asset;
            G.Log.Info($"LoadFonts: cached {assets.Length} from '{bundle.name}'");
        }
    }
}
