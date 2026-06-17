using System;
using System.IO;
using System.Linq;
using System.Text;
using TMPro;
using UnityEngine;
using static InsanityWorldMod.Core.Constants;
using static InsanityWorldMod.Core.DredgeHooks;
using static InsanityWorldMod.Core.Funcs;

namespace InsanityWorldMod.Core
{
    public static partial class Constants
    {
        public const string SCRIPT_ALPHABET = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        public const string SCRIPT_GLYPHS = SCRIPT_ALPHABET + "/\\.,:;-+=!?()%";

        public const string BUNDLES_SUBDIR = "Assets/Bundles";
        public const string BUNDLE_FONTS = "fonts";
    }

    public static partial class G
    {
        public static TMP_FontAsset InsanityFont { get; set; }
    }

    public static partial class Funcs
    {
        public static void InitInsanityScript() => InitInsanityScript(GetModBasePath());

        public static void InitInsanityScript(string modBasePath)
        {
            if (G.InsanityFont != null) return;

            try
            {
                string path = Path.Combine(modBasePath, BUNDLES_SUBDIR, BUNDLE_FONTS);
                if (!File.Exists(path))
                {
                    G.Log.Warn($"InitInsanityScript: font bundle not found at '{path}'");
                    return;
                }

                var bundle = AssetBundle.LoadFromFile(path);
                if (bundle == null)
                {
                    G.Log.Error($"InitInsanityScript: failed to load bundle '{path}'");
                    return;
                }

                G.InsanityFont = bundle.LoadAllAssets<TMP_FontAsset>().FirstOrDefault();
                if (G.InsanityFont == null)
                    G.Log.Error($"InitInsanityScript: no TMP_FontAsset inside bundle '{BUNDLE_FONTS}'");
                else
                    G.Log.Info($"InitInsanityScript: insanity font loaded '{G.InsanityFont.name}'");
            }
            catch (Exception ex)
            {
                G.Log.Error($"InitInsanityScript: {ex}");
            }
        }

        public static string Vig(string text, string key)
        {
            if (string.IsNullOrEmpty(text)) return string.Empty;

            string k = VigSanitizeKey(key);
            if (k.Length == 0) return text.ToUpperInvariant();

            int n = SCRIPT_ALPHABET.Length;
            var sb = new StringBuilder(text.Length);
            int ki = 0;
            for (int i = 0; i < text.Length; i++)
            {
                char ch = char.ToUpperInvariant(text[i]);
                int idx = SCRIPT_ALPHABET.IndexOf(ch);
                if (idx >= 0)
                {
                    int shift = SCRIPT_ALPHABET.IndexOf(k[ki % k.Length]) + 1;
                    sb.Append(SCRIPT_ALPHABET[(idx + shift) % n]);
                    ki++;
                }
                else
                {
                    sb.Append(text[i]);
                }
            }
            return sb.ToString();
        }

        private static string VigSanitizeKey(string key)
        {
            if (string.IsNullOrEmpty(key)) return string.Empty;
            string up = key.ToUpperInvariant();
            var sb = new StringBuilder(up.Length);
            for (int i = 0; i < up.Length; i++)
                if (SCRIPT_ALPHABET.IndexOf(up[i]) >= 0) sb.Append(up[i]);
            return sb.ToString();
        }
    }

    public class InsanityScriptSystem : IInsanityWorldSystem
    {
        public int Order => 9;

        public void OnLoad()
        {
            InitInsanityScript();
        }
    }
}
